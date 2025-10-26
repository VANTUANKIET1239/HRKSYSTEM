using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Common.Cookie
{
    using Microsoft.AspNetCore.Http;
    using System;

    public interface ICoreCookieService
    {
        void SetCookie(string key, string value, Expiration expirationType = Expiration.Day, int? expirationTime = null, bool isEssential = false);
        string? GetCookie(string key);
        void DeleteCookie(string key, bool isEssential);
    }

    public enum Expiration {
        Minute = 1,
        Hour = 2,
        Day = 3
    }   

    public class CoreCookieService : ICoreCookieService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CoreCookieService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void SetCookie(string key, string value, Expiration expirationType = Expiration.Day, int? expirationTime = null, bool isEssential = false)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = isEssential
            };

            if (expirationTime.HasValue)
            {
                switch (expirationTime)
                {
                    case (int)Expiration.Minute:
                        options.Expires = DateTimeOffset.UtcNow.AddMinutes(expirationTime.Value);
                        break;
                    case (int)Expiration.Hour: 
                        options.Expires = DateTimeOffset.UtcNow.AddHours(expirationTime.Value);
                        break;
                    case (int)Expiration.Day:
                        options.Expires = DateTimeOffset.UtcNow.AddDays(expirationTime.Value);
                        break;  
                }
            }

            _httpContextAccessor.HttpContext?.Response.Cookies.Append(key, value, options);
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

            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = isEssential
            };
            _httpContextAccessor.HttpContext?.Response.Cookies.Delete(key, options);
        }
    }

}
