using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AUTH.Application.Interfaces
{
    public class IdentityUserDto
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string UserName { get; set; } = null!;
    }

    public interface IIdentityService
    {
        Task<IdentityUserDto?> FindByNameOrEmailAsync(string identifier);
        
        Task<(bool Succeeded, bool IsLockedOut, bool RequiresTwoFactor)> CheckPasswordSignInAsync(string userId, string password, bool lockoutOnFailure);
        
        Task SignInWithClaimsAsync(string userId, bool isPersistent, IEnumerable<Claim> claims);
        
        Task ResetAccessFailedCountAsync(string userId);
        
        Task<(bool Succeeded, string? Errors)> CreateUserAsync(string email, string password);

        Task SignOutAsync();
    }
}
