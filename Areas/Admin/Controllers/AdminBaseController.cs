using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common.Constants;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

/// <summary>Mọi controller trong area Admin đều kế thừa class này nên tự động có phân quyền.</summary>
[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public abstract class AdminBaseController : Controller
{
}
