using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Web.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Core.Web.Filters;

public sealed class ApiResponseFilter : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not ObjectResult result) return;
        var response = result.Value as IApiResponse;
        if (response == null)
        {
            var status = result.StatusCode ?? 200;
            if (result.Value is ValidationProblemDetails validation)
                response = ApiResponsePolicy.Failure(context.HttpContext, status >= 400 ? status : 400,
                    "VALIDATION_ERROR", "Dữ liệu yêu cầu không hợp lệ.", "validation", new Dictionary<string, string[]>(validation.Errors));
            else if (status >= 400)
                response = ApiResponsePolicy.Failure(context.HttpContext, status, BaseResponse<object>.DefaultCode(status),
                    (result.Value as ProblemDetails)?.Title ?? "Request failed.");
            else
                response = BaseResponse<object>.SuccessResponse(result.Value!);
            result.Value = response;
            result.DeclaredType = response.GetType();
        }
        ApiResponsePolicy.Normalize(response, context.HttpContext,
            result.StatusCode is > 0 and not 200 ? result.StatusCode : response.StatusCode > 0 ? response.StatusCode : result.StatusCode);
        result.StatusCode = response.StatusCode;
        if (!response.Success)
        {
            // Ensure data is JSON null even when the original DTO is a value type.
            result.Value = new BaseResponse<object>
            {
                TraceId = response.TraceId, Success = false, StatusCode = response.StatusCode,
                Error = response.Error, Message = response.Message, Errors = response.Errors
            };
            result.DeclaredType = typeof(BaseResponse<object>);
        }
    }
    public void OnResultExecuted(ResultExecutedContext context) { }
}
