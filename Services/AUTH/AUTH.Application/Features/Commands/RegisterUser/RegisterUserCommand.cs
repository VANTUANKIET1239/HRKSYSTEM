using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Application.CQRS;
using FluentValidation;

namespace AUTH.Application.Features.Commands.RegisterUser
{

    public class RegisterUserCommand : ICommand<BaseResponse<RegisterUserResponse>>
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string FullName { get; set; }
        public string AppCode { get; set; } = "dcs-game";
    }

}
