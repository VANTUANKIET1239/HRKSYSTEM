using Core.Common.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Core.Web.Responses;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            return false;
        var status = exception is BusinessException business ? ApiResponsePolicy.StatusCode(business.Kind)
            : exception is UnauthorizedAccessException ? 401 : 500;
        var code = exception is BusinessException expected ? expected.Code : status == 401 ? "UNAUTHORIZED" : "INTERNAL_SERVER_ERROR";
        var message = exception is BusinessException ? exception.Message : status == 401
            ? "Authentication required." : "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";
        if (status >= 500)
            logger.LogError(exception, "Unhandled API failure. TraceId={TraceId}", ApiResponsePolicy.TraceId(context));
        else
            logger.LogDebug("API request rejected. Code={ErrorCode}, TraceId={TraceId}", code, ApiResponsePolicy.TraceId(context));
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(ApiResponsePolicy.Failure(context, status, code, message,
            validationErrors: (exception as RequestValidationException)?.Errors), cancellationToken);
        return true;
    }
}
