using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecondHandMarketplace.Web.Areas.User.Controllers;

/// <summary>Mọi controller trong area User kế thừa class này: chỉ cần đã đăng nhập (USER hoặc ADMIN).</summary>
[Area("User")]
[Authorize]
public abstract class UserBaseController : Controller
{
}
