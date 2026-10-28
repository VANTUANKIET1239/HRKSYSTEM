using Core.Common.Entity.MyCompany.Shared.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Web.Responses;

public static class ApiResponseServiceCollectionExtensions
{
    public static IServiceCollection AddHrkApiResponses(this IServiceCollection services)
    {
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddProblemDetails();
        services.Configure<ApiBehaviorOptions>(options => options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(entry => entry.Key, entry => entry.Value!.Errors.Select(error =>
                    string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Invalid value." : error.ErrorMessage).ToArray());
            return new BadRequestObjectResult(ApiResponsePolicy.Failure(context.HttpContext, 400,
                "VALIDATION_ERROR", "Dữ liệu yêu cầu không hợp lệ.", "validation", errors));
        });
        return services;
    }

    public static IApplicationBuilder UseHrkApiResponses(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages(async context =>
        {
            var status = context.HttpContext.Response.StatusCode;
            var message = status switch
            {
                401 => "Authentication required.", 
                403 => "Access denied.", 
                404 => "Resource not found.",
                _ => "Request failed."
            };
            var response = ApiResponsePolicy.Failure(context.HttpContext, status,
                BaseResponse<object>.DefaultCode(status), message);
            await ApiResponseLocalizer.ApplyAsync(response, context.HttpContext);
            await context.HttpContext.Response.WriteAsJsonAsync(response, context.HttpContext.RequestAborted);
        });
        return app;
    }
}
