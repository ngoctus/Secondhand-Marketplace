using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SecondHandMarketplace.Web.Models;
using SecondHandMarketplace.Web.Models.ViewModels.Home;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
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
        IMemoryCache cache) : Controller
    {
        private const string CacheKey = "home:catalog";
        private static readonly TimeSpan CacheTrangChu = TimeSpan.FromMinutes(5);

        private const int SoTinDuocVeMotLan = 60;
        private const int SoTinNoiBat = 4;
        private const int SoTinMotTab = 4;
        private const int SoTabToiDa = 6;

        public async Task<IActionResult> Index()
        {
            // Trang chủ hiển thị giống nhau cho mọi khách nên dùng chung một bản cache 5 phút.
            // Cố ý không truyền HttpContext.RequestAborted: nếu người dùng đóng tab giữa chừng,
            // truy vấn bị hủy và kết quả dở dang sẽ bị cache lại cho các request sau.
            if (!cache.TryGetValue(CacheKey, out HomeIndexVm? duLieu) || duLieu is null)
            {
                duLieu = await NapTrangChuAsync();

                // Chỉ cache khi lấy được sản phẩm: DB đang lỗi thì lần tải lại phải được thử thật,
                // không bị khóa một trang trống suốt 5 phút.
                if (duLieu.SanPhamNoiBat.Count > 0)
                    cache.Set(CacheKey, duLieu, CacheTrangChu);
            }

            return View(duLieu);
        }

        private async Task<HomeIndexVm> NapTrangChuAsync()
        {
            var danhMuc = await MocAnToan(() => categories.GetCategoriesAsync());
            var anhBanner = await MocAnToan(() => banners.LayBannerHienThiAsync());
            var danhGia = await MocAnToan(() => reviews.LayDanhGiaNoiBatAsync(SoTinNoiBat));

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
                DanhMucs = danhMuc,
                SanPhamNoiBat = tatCaTin.Take(SoTinNoiBat).ToList(),
                TabsDanhMuc = tabs,
                DanhGiaNoiBat = danhGia
            };
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
                400 => ("Yêu cầu không hợp lệ", "Dữ liệu gửi lên không hợp lệ. Vui lòng kiểm tra lại."),
                401 => ("Cần đăng nhập", "Bạn cần đăng nhập để tiếp tục."),
                403 => ("Không có quyền truy cập", "Bạn không có quyền truy cập trang này."),
                404 => ("Không tìm thấy trang", "Trang bạn tìm không tồn tại hoặc đã bị di chuyển."),
                _ => ("Đã xảy ra lỗi", "Hệ thống gặp sự cố. Vui lòng thử lại sau.")
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
