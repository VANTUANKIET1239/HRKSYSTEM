using AUTH.Infrastructure.Identity;
using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace HRK.AUTH.Features.Commands.LoginUser
{
    public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, BaseResponse<RegisterUserResponse>>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public RegisterUserCommandHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<BaseResponse<RegisterUserResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                NormalizedUserName = request.Email.ToLower(),
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                return new BaseResponse<RegisterUserResponse>
                {
                    Data = new RegisterUserResponse
                    {
                        Message = "User registered successfully."
                    },
                    Success = true
                };  
            }

            return new BaseResponse<RegisterUserResponse>
            {
                Data = new RegisterUserResponse
                {
                    Message = string.Join(", ", result.Errors.Select(e => e.Description))
                },
                Success = true
            };
        }

    }
}
