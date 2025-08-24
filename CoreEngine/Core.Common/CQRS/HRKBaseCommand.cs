using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

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

    }
}
