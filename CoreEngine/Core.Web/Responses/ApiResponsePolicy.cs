using System.Diagnostics;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.Errors;
using Microsoft.AspNetCore.Http;

namespace Core.Web.Responses;

public static class ApiResponsePolicy
{
    public static string TraceId(HttpContext context) => Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    public static int StatusCode(BusinessErrorKind kind) => kind switch
    {
        BusinessErrorKind.Validation => 400, 
        BusinessErrorKind.NotFound => 404,
        BusinessErrorKind.Unauthorized => 401, 
        BusinessErrorKind.Forbidden => 403, 
        _ => 409
    };

    public static void Normalize(IApiResponse response, HttpContext context, int? httpStatus = null)
    {
        var status = httpStatus ?? (response.StatusCode > 0 ? response.StatusCode : response.Success ? 200 : 400);
        if (!response.Success && status < 400) status = 400;
        response.Success = status is >= 200 and < 300;
        response.StatusCode = status;
        response.TraceId = TraceId(context);
        if (response.Success)
        {
            response.Error = null;
            response.Errors = null;
            return;
        }
        response.ClearData();
        if (status >= 500)
        {
            response.Message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";
            response.Errors = [response.Message];
            response.Error = new("INTERNAL_SERVER_ERROR", response.Message, "system");
        }
        else
        {
            var message = response.Error?.Message ?? response.Errors?.FirstOrDefault() ?? response.Message ?? "Request failed.";
            response.Error ??= new(BaseResponse<object>.DefaultCode(status), message, BaseResponse<object>.DefaultErrorType(status));
            response.Message = message;
            response.Errors ??= [message];
        }
    }

    public static BaseResponse<object> Failure(HttpContext context, int status, string code, string message,
        string? type = null, IReadOnlyDictionary<string, string[]>? validationErrors = null)
    {
        var response = BaseResponse<object>.FailResponse(message, statusCode: status, code: code, errorType: type);
        response.Error = response.Error! with { ValidationErrors = validationErrors };
        Normalize(response, context, status);
        return response;
    }
}
