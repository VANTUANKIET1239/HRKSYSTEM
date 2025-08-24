using FluentValidation;

using CoreEngine.CQRS;

namespace HRK.AUTH.Features.Commands.LoginUser
{
    public class LoginUserCommandValidation : AbstractValidator<LoginUserCommand>
    {

            public LoginUserCommandValidation()
            {
                RuleFor(x => x.Email)
                    .NotEmpty()
                    .EmailAddress();

                RuleFor(x => x.Password)
                    .NotEmpty()
                    .MinimumLength(6)
                    .WithMessage("Password must be at least 6 characters.");

            }
        
    }
}
