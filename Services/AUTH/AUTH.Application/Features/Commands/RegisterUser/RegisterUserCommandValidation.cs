using Core.Common.FluentValidation;

using CoreEngine.CQRS;
using FluentValidation;


namespace AUTH.Application.Features.Commands.RegisterUser
{
    public class RegisterUserCommandValidation : HRKValidator<RegisterUserCommand>
    {

            public RegisterUserCommandValidation()
            {
                NotEmpty(x => x.Email);
                Email(x => x.Email);

                NotEmpty(x => x.Password)
               .MinimumLength(6).WithMessage("Password must be at least 6 characters.");



        }
        
    }
}
