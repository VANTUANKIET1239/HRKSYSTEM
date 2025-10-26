using Core.Common.Entity.MyCompany.Shared.Responses;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Core.Common.Constants.Common;

namespace Core.Common.CQRS
{
    public interface IHRKBaseCommand
    {

    }
    public abstract class HRKBaseCommand 
    {
        protected readonly IHttpContextAccessor _httpContextAccessor;

        protected HRKBaseCommand(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor = httpContextAccessor;
        }


        protected HttpContext? HttpContext => _httpContextAccessor.HttpContext;

        protected ClaimsPrincipal? User => HttpContext?.User;

        protected string? UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        protected string? UserName => User?.Identity?.Name;

        protected string? IpAddress => HttpContext?.Connection?.RemoteIpAddress?.ToString();

        protected static BaseResponse<T> Response<T>(T data, bool success, string? message = null, int statusCode = 200)
        {
            return new BaseResponse<T>
            {
                Data = data,
                Success = success,
                Message = message,
                StatusCode = statusCode
            };
        }

       protected Guid GetCurrentLoginSession()
       {
            var user = HttpContext?.User;
            var sessionClaim = user?.FindFirst(Constants.Common.Constants.JSON_WEB_TOKEN.SESSIONID)?.Value;
            if (Guid.TryParse(sessionClaim, out Guid sessionId))
            {
                return sessionId;
            }
            throw new Exception("CurrentLoginSessionId claim is missing or invalid.");
        }
    }
}
