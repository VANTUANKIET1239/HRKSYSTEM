using Core.Application.Validation;

using Core.Application.CQRS;
using FluentValidation;


namespace AUTH.Application.Features.Commands.RegisterUser
{
    public class RegisterUserCommandValidation : HRKValidator<RegisterUserCommand>
    {

            public RegisterUserCommandValidation()
            {
                NotEmpty(x => x.Email);
                Email(x => x.Email);
                RuleFor(x => x.Email).MaximumLength(256);
                RuleFor(x => x.FullName).NotEmpty().MaximumLength(60)
                    .Must(value => value is not null && value.Trim().Length >= 3)
                    .WithMessage("Display name must contain between 3 and 60 characters.");
                RuleFor(x => x.AppCode).NotEmpty().MaximumLength(50);

                NotEmpty(x => x.Password)
               .MinimumLength(6).WithMessage("Password must be at least 6 characters.");



        }
        
    }
}
