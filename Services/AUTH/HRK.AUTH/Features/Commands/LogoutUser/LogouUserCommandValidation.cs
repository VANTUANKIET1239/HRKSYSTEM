using FluentValidation;

using CoreEngine.CQRS;
using Core.Common.FluentValidation;

namespace HRK.AUTH.Features.Commands.LoginUser
{
    public class LogoutCommandValidation : HRKValidator<LogouUserCommand>
    {

            public LogoutCommandValidation()
            {


            }
        
    }
}
