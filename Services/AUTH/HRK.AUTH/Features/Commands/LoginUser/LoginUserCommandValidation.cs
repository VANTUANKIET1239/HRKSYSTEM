using FluentValidation;

using CoreEngine.CQRS;
using Core.Common.FluentValidation;

namespace HRK.AUTH.Features.Commands.LoginUser
{
    public class LoginUserCommandValidation : HRKValidator<LoginUserCommand>
    {

        public LoginUserCommandValidation()
            {

                NotEmpty(x => x.Email);
                Email(x => x.Email);

                NotEmpty(x => x.Password)
                .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
                


            }
        
    }
}
