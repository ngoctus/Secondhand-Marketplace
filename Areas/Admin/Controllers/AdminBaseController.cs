using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web2Hand.Common.Constants;

namespace Web2Hand.Areas.Admin.Controllers;

/// <summary>Mọi controller trong area Admin đều kế thừa class này nên tự động có phân quyền.</summary>
[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public abstract class AdminBaseController : Controller
{
}
