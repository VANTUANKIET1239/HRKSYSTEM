using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AUTH.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace AUTH.Infrastructure.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IdentityService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IdentityUserDto?> FindByNameOrEmailAsync(string identifier)
        {
            var user = await _userManager.FindByNameAsync(identifier) 
                       ?? await _userManager.FindByEmailAsync(identifier);

            if (user == null) return null;

            return new IdentityUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty
            };
        }

        public async Task<(bool Succeeded, bool IsLockedOut, bool RequiresTwoFactor)> CheckPasswordSignInAsync(string userId, string password, bool lockoutOnFailure)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, false, false);

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure);

            return (result.Succeeded, result.IsLockedOut, result.RequiresTwoFactor);
        }

        public async Task SignInWithClaimsAsync(string userId, bool isPersistent, IEnumerable<Claim> claims)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            await _signInManager.SignInWithClaimsAsync(user, isPersistent, claims);
        }

        public async Task ResetAccessFailedCountAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            await _userManager.ResetAccessFailedCountAsync(user);
        }

        public async Task<(bool Succeeded, string? Errors)> CreateUserAsync(string email, string password)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                NormalizedUserName = email.ToUpper(),
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                return (true, null);
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, errors);
        }

        public async Task SignOutAsync()
        {
            await _signInManager.SignOutAsync();
        }
    }
}
