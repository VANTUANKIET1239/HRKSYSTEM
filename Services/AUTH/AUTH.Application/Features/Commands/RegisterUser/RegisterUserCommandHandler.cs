using AUTH.Application.Interfaces;
using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using MediatR;

namespace AUTH.Application.Features.Commands.RegisterUser
{
    public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, BaseResponse<RegisterUserResponse>>
    {
        private readonly IIdentityService _identityService;

        public RegisterUserCommandHandler(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task<BaseResponse<RegisterUserResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            var result = await _identityService.CreateUserAsync(request.Email, request.Password);

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
                    Message = result.Errors ?? "Registration failed."
                },
                Success = false
            };
        }
    }
}
