using Core.Common.Errors;
using Core.Web.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Core.Web.Filters;

/// <summary>Handle expected controller failures before exception middleware logs system failures.</summary>
public sealed class ApiBusinessExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var error = context.Exception as BusinessException;
        if (error == null && context.Exception is not UnauthorizedAccessException) return;
        if (error is not null) context.HttpContext.Items[typeof(BusinessException)] = error;
        var status = error == null ? 401 : ApiResponsePolicy.StatusCode(error.Kind);
        var response = ApiResponsePolicy.Failure(context.HttpContext, status,
            error?.Code ?? "UNAUTHORIZED", error?.Message ?? "Authentication required.",
            validationErrors: (error as RequestValidationException)?.Errors);
        context.Result = new ObjectResult(response) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
