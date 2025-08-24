using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using FluentValidation;

namespace HRK.AUTH.Features.Commands.LoginUser
{

    public class RegisterUserResponse
    {
         public string Message { get; set; }    
    }

    public class RegisterUserCommand : ICommand<BaseResponse<RegisterUserResponse>>
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string FullName { get; set; }
    }

}
