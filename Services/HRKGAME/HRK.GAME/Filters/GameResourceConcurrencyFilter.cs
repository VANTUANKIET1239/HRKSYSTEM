using Core.Web.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace HRK.GAME.Filters;

/// <summary>EF-specific conflicts stay in the game API, outside Core.Web.</summary>
public sealed class GameResourceConcurrencyFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not DbUpdateConcurrencyException) return;
        context.Result = new ObjectResult(ApiResponsePolicy.Failure(context.HttpContext, 409,
            "RESOURCE_CHANGED", "Tài nguyên vừa thay đổi. Vui lòng thử lại.")) { StatusCode = 409 };
        context.ExceptionHandled = true;
    }
}
