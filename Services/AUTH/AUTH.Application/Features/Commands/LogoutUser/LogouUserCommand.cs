using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AUTH.Application.Features.Commands.LogoutUser
{

    public class LogoutUserResponse
    {

        public bool LoggedOut { get; set; }
    }

    public class LogouUserCommand : ICommand<BaseResponse<LogoutUserResponse>>
    {

    }

}
