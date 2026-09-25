using FluentValidation;

using CoreEngine.CQRS;
using Core.Common.FluentValidation;

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
