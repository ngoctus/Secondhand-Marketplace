using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web2Hand.Common.Constants;

namespace Web2Hand.Areas.Seller.Controllers;

/// <summary>Mọi controller trong area Seller đều kế thừa class này nên tự động có phân quyền.</summary>
[Area("Seller")]
[Authorize(Roles = AppRoles.Seller)]
public abstract class SellerBaseController : Controller
{
}
