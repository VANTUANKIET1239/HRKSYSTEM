using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AUTH.Application.Features.Commands.LoginUser
{

    public class LoginUserResponse
    {

        public string UserId { get; set; }  

        public string UserName { get; set; }    

        public string UserEmail { get; set; }   



    }

    public class LoginUserCommand : ICommand<BaseResponse<LoginUserResponse>>
    {
        public required string Email { get; set; }

        [DataType(DataType.Password)]
        public required string Password { get; set; }

        public required bool RememberMe { get; set; }
    }

}
