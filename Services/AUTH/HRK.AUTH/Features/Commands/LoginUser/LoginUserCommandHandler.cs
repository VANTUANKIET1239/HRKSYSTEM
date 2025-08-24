using AUTH.Domain.Entities;
using AUTH.Infrastructure.Data;
using AUTH.Infrastructure.Identity;
using Core.Common.Constants.Common;
using Core.Common.Cookie;
using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.JwtHandler;
using Core.Common.JwtHandler.Entities;
using Core.Common.Repositories;
using CoreEngine.CQRS;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;

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

        private readonly string _apiKey;

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
            this._apiKey = configuration.GetValue<string>(Constants.HRK_API.API_KEY) ?? string.Empty;   
        }

        public async Task<BaseResponse<LoginUserResponse>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {

            //var kiet = await _unitOfWork.Repository<HRK_LoginSession>().GetAllAsync();
            //var kiet2 = await _unitOfWork.Repository<HRK_LoginSession>().GetAllAsync();
            //return new BaseResponse<LoginUserResponse> { Data = new LoginUserResponse { Message = "User not found." }, Success = false };

            try
            {
                var user = await _userManager.FindByNameAsync(request.Email)
                   ?? await _userManager.FindByEmailAsync(request.Email);


                if (user == null)
                    return new BaseResponse<LoginUserResponse> { Data = new LoginUserResponse { Message = "User not found." }, Success = false };


                var result = await _signInManager.PasswordSignInAsync(user.UserName, request.Password, request.RememberMe, lockoutOnFailure: true);

                if (!result.Succeeded)
                {
                    return new BaseResponse<LoginUserResponse> { Data = new LoginUserResponse { Message = "Password or Email was incorrect" }, Success = false };
                }



                Guid sessionId = Guid.NewGuid();


                var claims = new List<Claim>
                {
                    new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(Constants.JSON_WEB_TOKEN.SESSIONID, sessionId.ToString()),
                    new Claim(Constants.JSON_WEB_TOKEN.USERID, user.Id),
                };

                var token = _jwtCoreService.GenerateToken(claims);

                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponse<LoginUserResponse> { Data = new LoginUserResponse { Message = "Can not create JWT Token" }, Success = false };
                }


                var loginSession = new HRK_LoginSession
                {
                    UserId = user.Id,
                    AccessToken = token,
                    IPAddress = IpAddress
                };

                await _unitOfWork.Repository<HRK_LoginSession>().AddAsync(loginSession);

                await _unitOfWork.SaveChangesAsync(cancellationToken);



                _coreCookieService.SetCookie(Constants.JSON_WEB_TOKEN.SESSIONID, sessionId.ToString(), Expiration.Day,1, true);
                _coreCookieService.SetCookie(Constants.JSON_WEB_TOKEN.JWT,token, Expiration.Minute, _jwtOptions.Value.ExpiryMinutes, true);
                _coreCookieService.SetCookie(Constants.HRK_API.API_KEY, _apiKey, Expiration.Day, 1, true);


                if (result.IsLockedOut)
                {
                    return new BaseResponse<LoginUserResponse> { Data = new LoginUserResponse { Message = "User is locked out due to multiple failed attempts." }, Success = false };
                }
                // reset failedLogin 
                await _userManager.ResetAccessFailedCountAsync(user);

                return new BaseResponse<LoginUserResponse>
                {
                    Data = new LoginUserResponse
                    {
                        Message = "Login successful",
                    },
                    Success = true
                };


            }
            catch (Exception ex)
            {
                return new BaseResponse<LoginUserResponse>
                {
                    Data = new LoginUserResponse { Message = ex.Message },
                    Success = false
                };
            }
        }


    
    }
}
