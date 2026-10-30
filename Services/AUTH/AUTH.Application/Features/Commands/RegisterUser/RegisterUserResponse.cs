using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Application.CQRS;
using FluentValidation;

namespace AUTH.Application.Features.Commands.RegisterUser
{

    public class RegisterUserResponse
    {
         public string Message { get; set; } = "";
    }

}
