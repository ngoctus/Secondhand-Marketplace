using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Areas.Admin.Controllers;

public class AuditLogsController : AdminBaseController
{
    // TODO: Nhật ký hoạt động (Audit Log)
    public IActionResult Index() => View();
}
