using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Models;
using SecondHandMarketplace.Web.Models.ViewModels.Auctions;
using SecondHandMarketplace.Web.Models.ViewModels.Home;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Services.Auctions;
using SecondHandMarketplace.Web.Services.Banners;
using SecondHandMarketplace.Web.Services.Categories;
using SecondHandMarketplace.Web.Services.Products;
using SecondHandMarketplace.Web.Services.Reviews;
using System.Diagnostics;

namespace SecondHandMarketplace.Web.Controllers
{
    public class HomeController(
        IProductService products,
        ICategoryService categories,
        IBannerService banners,
        IReviewService reviews,
        IAuctionService auctions,
        IMemoryCache cache,
        SecondHandMarketplace.Web.Data.AppDbContext db) : Controller
    {
        public const string CatalogCacheKey = HomeCacheHelper.CatalogCacheKey;
        private static readonly TimeSpan CacheTrangChu = HomeCacheHelper.CacheTrangChu;

        private const int SoTinDuocVeMotLan = 60;
        private const int SoTinNoiBat = 4;
        private const int SoTinMotTab = 4;
        private const int SoTabToiDa = 6;

        public async Task<IActionResult> Index()
        {
            // Trang chủ hiển thị giống nhau cho mọi khách nên dùng chung một bản cache 5 phút.
            // Cố ý không truyền HttpContext.RequestAborted: nếu người dùng đóng tab giữa chừng,
            // truy vấn bị hủy và kết quả dở dang sẽ bị cache lại cho các request sau.
            if (!cache.TryGetValue(CatalogCacheKey, out HomeIndexVm? duLieu) || duLieu is null)
            {
                duLieu = await NapTrangChuAsync();

                // Chỉ cache khi lấy được sản phẩm: DB đang lỗi thì lần tải lại phải được thử thật,
                // không bị khóa một trang trống suốt 5 phút.
                if (duLieu.SanPhamNoiBat.Count > 0)
                    cache.Set(CatalogCacheKey, duLieu, CacheTrangChu);
            }

            return View(duLieu);
        }

        private async Task<HomeIndexVm> NapTrangChuAsync()
        {
            var danhMuc = await MocAnToan(() => categories.GetCategoriesAsync());
            var anhBanner = await MocAnToan(() => banners.LayBannerHienThiAsync());
            var danhGia = await MocAnToan(() => reviews.LayDanhGiaNoiBatAsync(SoTinNoiBat));

            // Nạp phiên đấu giá nổi bật cho khối dưới Hero: ưu tiên phiên đang diễn ra, thiếu thì bù phiên sắp diễn ra.
            var phienDangChay = await MocAnToan(async () =>
                (await auctions.SearchPublicAsync("dang-dien-ra", null, 1, SoTinNoiBat)).Items);
            IReadOnlyList<AuctionCardVm> phienDauGia = phienDangChay;
            if (phienDangChay.Count < SoTinNoiBat)
            {
                var phienSapMo = await MocAnToan(async () =>
                    (await auctions.SearchPublicAsync("sap-dien-ra", null, 1, SoTinNoiBat - phienDangChay.Count)).Items);
                phienDauGia = phienDangChay.Concat(phienSapMo).Take(SoTinNoiBat).ToList();
            }

            // Một query duy nhất lấy 60 tin mới nhất, mọi khối còn lại tách trong bộ nhớ.
            // Trước đây view gọi SearchAsync rồi lặp theo từng danh mục nên sinh N+1.
            var tatCaTin = await MocAnToan(async () =>
                (await products.SearchAsync(new ProductSearchVm { PageSize = SoTinDuocVeMotLan })).Items);

            var tabs = tatCaTin
                .GroupBy(t => t.CategoryId)
                .Where(nhom => nhom.Key is not null && nhom.Count() >= SoTinMotTab)
                // GroupBy giữ thứ tự khóa xuất hiện đầu tiên và danh sách đã sắp theo tin mới nhất,
                // nên tab đầu luôn là danh mục vừa có tin mới — không cần sắp thêm.
                .Take(SoTabToiDa)
                .Select(nhom => new HomeCategoryTabVm(
                    nhom.Key!.Value,
                    nhom.First().CategoryName ?? "Khác",
                    nhom.Take(SoTinMotTab).ToList()))
                .ToList();

            return new HomeIndexVm
            {
                Banners = anhBanner,
                PhienDauGiaNoiBat = phienDauGia,
                DanhMucs = danhMuc,
                SanPhamNoiBat = ChonSanPhamGoiY(tatCaTin, SoTinNoiBat, daDangDanhMuc: true),
                TabsDanhMuc = tabs,
                DanhGiaNoiBat = danhGia,
                TongSanPham = await DemAnToan(() => db.SanPhams.CountAsync(p => p.TrangThai == "ACTIVE" && !p.DaXoa)),
                TongNguoiBan = await DemAnToan(() => db.CuaHangs.CountAsync(c => c.TrangThai == "ACTIVE")),
                TongDonHoanTat = await DemAnToan(() => db.DonHangs.CountAsync(d => d.TrangThai == "COMPLETED"))
            };
        }

        /// <summary>
        /// Trả về khối lưới sản phẩm gợi ý (4 thẻ) ngay trên trang chủ khi người dùng bấm
        /// Mặc định / Mới đăng / Giá tốt / Giá tiết kiệm hoặc chuyển danh mục.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SanPhamGoiY(string? mode = null, int? categoryId = null)
        {
            var cheDo = (mode ?? "default").Trim().ToLowerInvariant();
            IReadOnlyList<ProductCardVm> ketQua = cheDo switch
            {
                "newest" => await MocAnToan(async () =>
                    (await products.SearchAsync(new ProductSearchVm
                    {
                        CategoryId = categoryId,
                        Sort = "newest",
                        PageSize = SoTinNoiBat
                    })).Items),
                "best_price" => await MocAnToan(async () =>
                    (await products.SearchAsync(new ProductSearchVm
                    {
                        CategoryId = categoryId,
                        PriceRange = 1,
                        Sort = "newest",
                        PageSize = SoTinNoiBat
                    })).Items),
                "bargain" => await MocAnToan(async () =>
                    (await products.SearchAsync(new ProductSearchVm
                    {
                        CategoryId = categoryId,
                        Sort = "price_asc",
                        PageSize = SoTinNoiBat
                    })).Items),
                _ => await LayGoiYMacDinhAsync(categoryId)
            };

            return PartialView("_HomeRecGrid", ketQua);
        }

        private async Task<IReadOnlyList<ProductCardVm>> LayGoiYMacDinhAsync(int? categoryId)
        {
            var nguon = await MocAnToan(async () =>
                (await products.SearchAsync(new ProductSearchVm
                {
                    CategoryId = categoryId,
                    PageSize = categoryId is null ? SoTinDuocVeMotLan : 24
                })).Items);

            return ChonSanPhamGoiY(nguon, SoTinNoiBat, daDangDanhMuc: categoryId is null);
        }

        /// <summary>
        /// Chọn sản phẩm cho chế độ "Mặc định" (gợi ý nổi bật): ưu tiên tin có ảnh và shop đánh giá tốt,
        /// đồng thời rải đều theo danh mục khi đang xem tất cả để khác biệt với chế độ "Mới đăng".
        /// </summary>
        private static IReadOnlyList<ProductCardVm> ChonSanPhamGoiY(
            IReadOnlyList<ProductCardVm> nguon,
            int soLuong,
            bool daDangDanhMuc)
        {
            if (nguon.Count <= soLuong)
                return nguon.ToList();

            var sapXep = nguon
                .Select((item, index) => new { item, index })
                .OrderByDescending(x => !string.IsNullOrWhiteSpace(x.item.ImageUrl))
                .ThenByDescending(x => x.item.DiemTrungBinh ?? 0m)
                .ThenByDescending(x => x.item.SoDanhGia)
                .ThenBy(x => x.index)
                .Select(x => x.item)
                .ToList();

            if (!daDangDanhMuc)
                return sapXep.Take(soLuong).ToList();

            var ketQua = new List<ProductCardVm>(soLuong);
            var danhMucDaChon = new HashSet<int>();

            foreach (var sp in sapXep)
            {
                if (ketQua.Count >= soLuong) break;
                if (sp.CategoryId is int maDm && danhMucDaChon.Add(maDm))
                    ketQua.Add(sp);
            }

            if (ketQua.Count < soLuong)
            {
                var idDaChon = new HashSet<int>(ketQua.Select(p => p.Id));
                foreach (var sp in sapXep)
                {
                    if (ketQua.Count >= soLuong) break;
                    if (idDaChon.Add(sp.Id))
                        ketQua.Add(sp);
                }
            }

            return ketQua;
        }

        // Ba con số thống kê chỉ mang tính trang trí: thiếu bảng thì hiện 0 chứ không làm trang chủ 500.
        private static async Task<int> DemAnToan(Func<Task<int>> dem)
        {
            try
            {
                return await dem();
            }
            catch
            {
                return 0;
            }
        }

        // Trang chủ phải mở được cả khi database chưa chạy đủ script (thiếu bảng Banner/DanhGia),
        // nên mỗi khối lỗi thì chỉ khối đó trống, không làm cả trang nổ 500.
        private static async Task<IReadOnlyList<T>> MocAnToan<T>(Func<Task<IReadOnlyList<T>>> hopThu)
        {
            try
            {
                return await hopThu();
            }
            catch
            {
                return [];
            }
        }

        [HttpGet("/gioi-thieu")]
        [HttpGet("/ve-chung-toi")]
        public IActionResult About()
        {
            return View();
        }

        [HttpGet("/faq")]
        [HttpGet("/huong-dan-giao-dich")]
        public IActionResult Faq()
        {
            return View();
        }

        [HttpGet("/dieu-khoan")]
        [HttpGet("/chinh-sach-bao-mat")]
        public IActionResult Terms()
        {
            return View();
        }

        // Lỗi hệ thống bất ngờ (UseExceptionHandler)
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return TaoTrangLoi(500);
        }

        // 400, 403, 404... (UseStatusCodePagesWithReExecute)
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult TrangLoi(int ma)
        {
            return TaoTrangLoi(ma);
        }

        // Đã đăng nhập nhưng sai quyền (AccessDeniedPath của cookie)
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult KhongCoQuyen()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return TaoTrangLoi(403);
        }

        private IActionResult TaoTrangLoi(int ma)
        {
            var (tieuDe, thongDiep) = ma switch
            {
                400 => (
                    HienThi.Chon("Yêu cầu không hợp lệ", "Bad Request"),
                    HienThi.Chon("Dữ liệu gửi lên không hợp lệ. Vui lòng kiểm tra lại.", "The submitted request is invalid. Please check and try again.")),
                401 => (
                    HienThi.Chon("Cần đăng nhập", "Authentication Required"),
                    HienThi.Chon("Bạn cần đăng nhập để tiếp tục.", "Please sign in to continue.")),
                403 => (
                    HienThi.Chon("Không có quyền truy cập", "Access Denied"),
                    HienThi.Chon("Bạn không có quyền truy cập trang này.", "You do not have permission to access this page.")),
                404 => (
                    HienThi.Chon("Không tìm thấy trang", "Page Not Found"),
                    HienThi.Chon("Trang bạn tìm không tồn tại hoặc đã bị di chuyển.", "The page you are looking for does not exist or has been moved.")),
                _ => (
                    HienThi.Chon("Đã xảy ra lỗi", "An Error Occurred"),
                    HienThi.Chon("Hệ thống gặp sự cố. Vui lòng thử lại sau.", "Something went wrong on our end. Please try again later."))
            };

            return View("Error", new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                MaLoi = ma,
                TieuDe = tieuDe,
                ThongDiep = thongDiep
            });
        }
    }
}
