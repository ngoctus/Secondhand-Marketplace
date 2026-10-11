using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web2Hand.Common.Constants;

namespace Web2Hand.Areas.Buyer.Controllers;

/// <summary>Mọi controller trong area Buyer đều kế thừa class này nên tự động có phân quyền.</summary>
[Area("Buyer")]
[Authorize(Roles = AppRoles.Buyer)]
public abstract class BuyerBaseController : Controller
{
}
