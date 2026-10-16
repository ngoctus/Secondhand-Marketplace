using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Models;
using System.Diagnostics;

namespace SecondHandMarketplace.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
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
