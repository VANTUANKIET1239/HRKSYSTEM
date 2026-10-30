using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Web.Cookies
{
    using Core.Web.Cookies;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using System;

    public interface ICoreCookieService
    {
        void SetCookie(string key, string value, Expiration expirationType = Expiration.Day, int? expirationTime = null, bool isEssential = false);
        string? GetCookie(string key);
        void DeleteCookie(string key, bool isEssential);
    }

}
