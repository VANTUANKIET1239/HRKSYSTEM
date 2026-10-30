using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Application.CQRS;
using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AUTH.Application.Features.Commands.LoginUser
{

    public class LoginUserCommand : ICommand<BaseResponse<LoginUserResponse>>
    {
        public required string Email { get; set; }

        [DataType(DataType.Password)]
        public required string Password { get; set; }

        public required bool RememberMe { get; set; }
        public string? Audience { get; set; }
    }

}
