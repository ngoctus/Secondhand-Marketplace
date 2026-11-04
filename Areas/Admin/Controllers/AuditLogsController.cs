using Microsoft.AspNetCore.Mvc;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

public class AuditLogsController : AdminBaseController
{
    // TODO: Nhật ký hoạt động (Audit Log)
    public IActionResult Index() => View();
}
