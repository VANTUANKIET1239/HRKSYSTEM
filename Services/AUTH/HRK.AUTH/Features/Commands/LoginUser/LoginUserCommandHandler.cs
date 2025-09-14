using AUTH.Domain.Entities;
using AUTH.Infrastructure.Data;
using AUTH.Infrastructure.Identity;
using Azure.Core;
using Core.Common.Constants.Common;
using Core.Common.Cookie;
using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.JwtHandler;
using Core.Common.JwtHandler.Entities;
using Core.Common.Repositories;
using CoreEngine.CQRS;
using HRK.AUTH.Helpers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using static System.Net.WebRequestMethods;

namespace HRK.AUTH.Features.Commands.LoginUser
{
    public class LoginUserCommandHandler : HRKBaseCommand, ICommandHandler<LoginUserCommand, BaseResponse<LoginUserResponse>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtCoreService _jwtCoreService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUnitOfWork<ApplicationDbContext> _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly ICoreCookieService _coreCookieService;
        private readonly IOptions<JwtSettings> _jwtOptions;

   //     private readonly string _apiKey;

        public LoginUserCommandHandler(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtCoreService jwtCoreService,
            IHttpContextAccessor httpContextAccessor,
            IUnitOfWork<ApplicationDbContext> unitOfWork,
            IConfiguration configuration,
            ICoreCookieService coreCookieService,
            IOptions<JwtSettings> options
            ) : base(httpContextAccessor)
        {
            _userManager = userManager;
            this._signInManager = signInManager;
            this._jwtCoreService = jwtCoreService;
            this._httpContextAccessor = httpContextAccessor;
            this._unitOfWork = unitOfWork;
            this._configuration = configuration;
            this._coreCookieService = coreCookieService;
            this._jwtOptions = options;
        }

        public async Task<BaseResponse<LoginUserResponse>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(request.Email)
                   ?? await _userManager.FindByEmailAsync(request.Email);

                if (user == null)
                {
                      return new BaseResponse<LoginUserResponse> { Data = new LoginUserResponse(), Success = false, StatusCode = 404, Message = "User not found." };

                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

                if (result.IsLockedOut)
                {
                    return new BaseResponse<LoginUserResponse> {Data = new LoginUserResponse(), Success = false, StatusCode = 403, Message = "Account locked. Try again later." };
                }

                if (result.RequiresTwoFactor)
                {
                    return new BaseResponse<LoginUserResponse> {Data = new LoginUserResponse(), Success = false, StatusCode = 401, Message = "Two-factor required." };
                }

                if (!result.Succeeded)
                {
                    return new BaseResponse<LoginUserResponse> {Data = new LoginUserResponse(), Success = false, StatusCode = 401, Message = "Invalid credentials." };
                }

                Guid sessionId = Guid.NewGuid();
                Guid Jti = Guid.NewGuid();  

                var claims = new List<Claim>
                {
                    new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, user.Id),
                    new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Jti.ToString()),
                    new Claim(Constants.JSON_WEB_TOKEN.SESSIONID, sessionId.ToString()),
                    new Claim(Constants.JSON_WEB_TOKEN.USERID, user.Id)
                };

             //   var accessToken = _jwtCoreService.GenerateToken(claims);

                //if (string.IsNullOrEmpty(accessToken.AccessToken))
                //{
                //    return new BaseResponse<LoginUserResponse> {Data = new LoginUserResponse(), Success = false, StatusCode = 500, Message = "Cannot create JWT Token" };
                //}

                var rawRefreshToken = _jwtCoreService.CreateRefreshToken(user.Id);

                await _unitOfWork.BeginTransactionAsync();

                var now = DateTime.UtcNow;

            
                await _unitOfWork.Repository<HRK_RefreshToken>().AddAsync(new HRK_RefreshToken
                {
                    TokenHash = DEncryptHelpers.Hash(rawRefreshToken),
                    UserId = user.Id,
                    CreatedAt = now,
                    CreatedByIp = IpAddress,
                    ExpiresAt = now.AddDays(_jwtOptions.Value.RefreshTokenDays),
                    SessionId = sessionId
                });

                await _unitOfWork.Repository<HRK_LoginSession>().AddAsync(new HRK_LoginSession
                {
                    UserId = user.Id,
                    IPAddress = IpAddress,
                    SessionId = sessionId,
                    UserAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString()
                });

                await _userManager.ResetAccessFailedCountAsync(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                //_coreCookieService.SetCookie(Constants.JSON_WEB_TOKEN.SESSIONID, sessionId.ToString(), Expiration.Day, 1, true);
                //_coreCookieService.SetCookie(Constants.JSON_WEB_TOKEN.JWT, accessToken.AccessToken, Expiration.Minute, _jwtOptions.Value.ExpiryMinutes, true);
                _coreCookieService.SetCookie(Constants.JSON_WEB_TOKEN.REFRESHTOKEN, rawRefreshToken, Expiration.Day, _jwtOptions.Value.RefreshTokenDays, true);

                return new BaseResponse<LoginUserResponse>
                {
                    Data = new LoginUserResponse
                    {
                        UserEmail = user.Email,
                        UserId = user.Id,
                        UserName = user.UserName,
                    },
                    Success = true,
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                return new BaseResponse<LoginUserResponse>
                {
                    Data = new LoginUserResponse(),
                    Success = false,
                    StatusCode = 500,
                    Message = ex.Message
                };
            }
        }



    }
}
