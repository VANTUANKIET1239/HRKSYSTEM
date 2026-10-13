using Core.Web.Filters;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Filters;

public static class GameFilterExtensions
{
    public static MvcOptions AddGameApiFilters(this MvcOptions options)
    {
        options.AddHrkApiFilters();
        options.Filters.Add<GameResourceConcurrencyFilter>();
        return options;
    }
}
