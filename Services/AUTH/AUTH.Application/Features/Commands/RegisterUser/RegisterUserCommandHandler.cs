using AUTH.Application.Interfaces;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Application.CQRS;
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
                return BaseResponse<RegisterUserResponse>.SuccessResponse(new RegisterUserResponse
                {
                    Message = "User registered successfully."
                });
            }

            return BaseResponse<RegisterUserResponse>.FailResponse(result.Errors ?? "Registration failed.",
                statusCode: 400, code: "REGISTRATION_REJECTED", errorType: "business");
        }
    }
}
