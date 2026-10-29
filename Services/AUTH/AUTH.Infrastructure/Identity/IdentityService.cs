using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AUTH.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using AUTH.Infrastructure.Data;
using AUTH.Domain.Entities;
using Core.Persistence.Repositories;
using Core.Messaging.Contracts;
using Core.TransactionalMessaging.Outbox;
using Microsoft.EntityFrameworkCore;

namespace AUTH.Infrastructure.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUnitOfWork<AuthDbContext> _unitOfWork;
        private readonly IUnitOfWork<ApplicationDbContext> _application;
        private readonly IOutboxWriter _outbox;

        public IdentityService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
            IUnitOfWork<AuthDbContext> unitOfWork, IUnitOfWork<ApplicationDbContext> application, IOutboxWriter outbox)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _unitOfWork = unitOfWork;
            _application = application;
            _outbox = outbox;
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

        public async Task<(bool Succeeded, string? Errors)> CreateUserAsync(string email, string password,
            string fullName, string appCode, CancellationToken ct = default)
        {
            if (!await _application.ReadOnlyRepository<HRK_ApplicationRouteConfig>().Query()
                .AnyAsync(x => x.AppCode == appCode && x.IsActive, ct))
                return (false, "Unknown or inactive application.");

            var userId = Guid.NewGuid().ToString();
            var eventId = Guid.NewGuid();
            (bool Succeeded, string? Errors) outcome = (false, null);
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Recover an ambiguous commit on an execution-strategy retry of this same call.
                if (await _unitOfWork.ReadOnlyRepository<ApplicationUser>().Query().AnyAsync(x => x.Id == userId, ct))
                {
                    outcome = (true, null);
                    return;
                }
                var user = new ApplicationUser { Id = userId, UserName = email, Email = email, FullName = fullName };
                var result = await _userManager.CreateAsync(user, password);
                outcome = (result.Succeeded, result.Succeeded ? null : string.Join(", ", result.Errors.Select(e => e.Description)));
                if (!result.Succeeded) return;
                _outbox.Add(new AccountRegisteredV1(eventId, user.Id, appCode, fullName, DateTimeOffset.UtcNow),
                    AccountContractNames.RegisteredV1, AccountContractNames.Publisher,
                    AccountContractNames.RegisteredV1, partitionKey: user.Id);
            }, ct);
            return outcome;
        }

        public async Task SignOutAsync()
        {
            await _signInManager.SignOutAsync();
        }
    }
}
