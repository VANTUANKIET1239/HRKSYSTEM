using Microsoft.AspNetCore.Mvc;

namespace Core.Web.Filters;

public static class ApiFilterExtensions
{
    /// <summary>Registers the shared business exception and response normalization filters.</summary>
    public static MvcOptions AddHrkApiFilters(this MvcOptions options)
    {
        options.Filters.Add<ApiBusinessExceptionFilter>();
        options.Filters.Add<ApiResponseFilter>();
        return options;
    }
}
