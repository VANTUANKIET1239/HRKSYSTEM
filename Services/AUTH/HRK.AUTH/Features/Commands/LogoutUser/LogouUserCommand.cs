using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace HRK.AUTH.Features.Commands.LoginUser
{

    public class LogoutUserResponse
    {

        public bool LoggedOut { get; set; }
    }

    public class LogouUserCommand : ICommand<BaseResponse<LogoutUserResponse>>
    {

    }

}
