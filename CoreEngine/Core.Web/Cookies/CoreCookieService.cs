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

    public class CoreCookieService : ICoreCookieService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;


        private readonly CookieOptions _defaultCookieOptions;
        public CoreCookieService(IHttpContextAccessor httpContextAccessor, IOptions<HRKCookieOptions> cookieOptions)
        {
            _httpContextAccessor = httpContextAccessor;
            _defaultCookieOptions = new CookieOptions
            {
                HttpOnly = cookieOptions.Value.HttpOnly,
                Secure = cookieOptions.Value.Secure,
                SameSite = SameSiteMode.None,
                IsEssential = cookieOptions.Value.IsEssential
            };
        }

        public void SetCookie(string key, string value, Expiration expirationType = Expiration.Day, int? expirationTime = null, bool isEssential = false)
        {


            if (expirationTime.HasValue)
            {
                switch (expirationType)
                {
                    case Expiration.Minute:
                        _defaultCookieOptions.Expires = DateTimeOffset.UtcNow.AddMinutes(expirationTime.Value);
                        break;
                    case Expiration.Hour:
                        _defaultCookieOptions.Expires = DateTimeOffset.UtcNow.AddHours(expirationTime.Value);
                        break;
                    case Expiration.Day:
                        _defaultCookieOptions.Expires = DateTimeOffset.UtcNow.AddDays(expirationTime.Value);
                        break;  
                }
            }

            _httpContextAccessor.HttpContext?.Response.Cookies.Append(key, value, _defaultCookieOptions);
        }

        public string? GetCookie(string key)
        {
            if (_httpContextAccessor.HttpContext?.Request.Cookies is not null &&
                _httpContextAccessor.HttpContext.Request.Cookies.TryGetValue(key, out var value))
            {
                return value;
            }

            return null;
        }

        public void DeleteCookie(string key, bool isEssential)
        {
            _httpContextAccessor.HttpContext?.Response.Cookies.Delete(key, _defaultCookieOptions);
        }
    }

}
