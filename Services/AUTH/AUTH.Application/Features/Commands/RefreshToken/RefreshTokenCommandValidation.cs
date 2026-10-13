using FluentValidation;

using Core.Application.CQRS;
using Core.Application.Validation;

namespace AUTH.Application.Features.Commands.RefreshToken
{
    public class RefreshTokenCommandValidation : HRKValidator<RefreshTokenCommand>
    {

        public RefreshTokenCommandValidation()
        {
            NotEmpty(x => x.Audience)
                .MaximumLength(100);

        }

    }
}
