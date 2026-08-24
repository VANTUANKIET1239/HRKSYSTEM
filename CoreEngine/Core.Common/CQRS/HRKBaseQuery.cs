using Core.Common.Constants.Common;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Core.Common.CQRS
{
    public interface IHRKBaseQuery
    {
    }

    public abstract class HRKBaseQuery
    {
        protected readonly IHttpContextAccessor _httpContextAccessor;

        protected HRKBaseQuery(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected HttpContext? HttpContext => _httpContextAccessor.HttpContext;

        protected ClaimsPrincipal? User => HttpContext?.User;

        protected string? UserId =>
            User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User?.FindFirst(Constants.Common.Constants.JSON_WEB_TOKEN.USERID)?.Value
            ?? User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

        protected string? UserName => User?.Identity?.Name;

        protected string? IpAddress => HttpContext?.Connection?.RemoteIpAddress?.ToString();

        protected string GetUserId()
        {
            var userId = UserId;
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated or UserId claim is missing.");
            }
            return userId;
        }

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
