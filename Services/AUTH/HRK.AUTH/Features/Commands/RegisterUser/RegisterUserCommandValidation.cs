using FluentValidation;

using CoreEngine.CQRS;

namespace HRK.AUTH.Features.Commands.LoginUser
{
    public class RegisterUserCommandValidation : AbstractValidator<RegisterUserCommand>
    {

            public RegisterUserCommandValidation()
            {
                RuleFor(x => x.Email)
                    .NotEmpty()
                    .EmailAddress();

                RuleFor(x => x.Password)
                    .NotEmpty()
                    .MinimumLength(6)
                    .WithMessage("Password must be at least 6 characters.");

                RuleFor(x => x.FullName)
                    .NotEmpty();
            }
        
    }
}
