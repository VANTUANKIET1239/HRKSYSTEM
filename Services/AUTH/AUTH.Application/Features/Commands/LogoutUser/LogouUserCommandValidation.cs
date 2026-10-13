using FluentValidation;

using Core.Application.CQRS;
using Core.Application.Validation;

namespace AUTH.Application.Features.Commands.LogoutUser
{
    public class LogoutCommandValidation : HRKValidator<LogouUserCommand>
    {

            public LogoutCommandValidation()
            {


            }
        
    }
}
