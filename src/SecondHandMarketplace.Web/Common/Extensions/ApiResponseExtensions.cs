using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common.Constants;

namespace SecondHandMarketplace.Web.Common.Extensions;

/// <summary>
/// Extension methods giúp API Controller chuyển đổi ServiceResult sang HTTP ActionResult chuẩn RESTful.
/// </summary>
public static class ApiResponseExtensions
{
    public static ActionResult ToActionResult<T>(this ServiceResult<T> result, ControllerBase controller)
    {
        if (result.Success)
        {
            return controller.Ok(result);
        }

        return result.Code switch
        {
            ErrorCodes.NotFound 
                or ErrorCodes.ProductNotFound 
                or ErrorCodes.UserNotFound 
                or ErrorCodes.OrderNotFound 
                or ErrorCodes.ShopNotFound 
                or ErrorCodes.CategoryNotFound 
                or ErrorCodes.AuctionNotFound
                or ErrorCodes.CartItemNotFound
                or ErrorCodes.DisputeNotFound
                => controller.NotFound(result),

            ErrorCodes.Unauthorized 
                or ErrorCodes.InvalidCredentials 
                => controller.Unauthorized(result),

            ErrorCodes.Forbidden 
                => controller.StatusCode(403, result),

            ErrorCodes.Conflict 
                or ErrorCodes.EmailExists 
                or ErrorCodes.PhoneExists 
                or ErrorCodes.ShopExists
                => controller.Conflict(result),

            ErrorCodes.InternalError 
                => controller.StatusCode(500, result),

            _ => controller.BadRequest(result)
        };
    }

    public static ActionResult ToActionResult(this ServiceResult result, ControllerBase controller)
    {
        if (result.Success)
        {
            return controller.Ok(result);
        }

        return result.Code switch
        {
            ErrorCodes.NotFound 
                or ErrorCodes.ProductNotFound 
                or ErrorCodes.UserNotFound 
                or ErrorCodes.OrderNotFound 
                or ErrorCodes.ShopNotFound 
                or ErrorCodes.CategoryNotFound 
                or ErrorCodes.AuctionNotFound
                or ErrorCodes.CartItemNotFound
                or ErrorCodes.DisputeNotFound
                => controller.NotFound(result),

            ErrorCodes.Unauthorized 
                or ErrorCodes.InvalidCredentials 
                => controller.Unauthorized(result),

            ErrorCodes.Forbidden 
                => controller.StatusCode(403, result),

            ErrorCodes.Conflict 
                or ErrorCodes.EmailExists 
                or ErrorCodes.PhoneExists 
                or ErrorCodes.ShopExists
                => controller.Conflict(result),

            ErrorCodes.InternalError 
                => controller.StatusCode(500, result),

            _ => controller.BadRequest(result)
        };
    }
}
