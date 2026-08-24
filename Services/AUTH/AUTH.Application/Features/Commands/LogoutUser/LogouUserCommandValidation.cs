using FluentValidation;

using CoreEngine.CQRS;
using Core.Common.FluentValidation;

namespace AUTH.Application.Features.Commands.LogoutUser
{
    public class LogoutCommandValidation : HRKValidator<LogouUserCommand>
    {

            public LogoutCommandValidation()
            {


            }
        
    }
}
