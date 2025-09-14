using FluentValidation;

using CoreEngine.CQRS;
using Core.Common.FluentValidation;

namespace HRK.AUTH.Features.Commands.RefreshToken
{
    public class RefreshTokenCommandValidation : HRKValidator<RefreshTokenCommand>
    {

        private static List<string> allowedAudiences;

        public RefreshTokenCommandValidation(IConfiguration configuration)
        {
            allowedAudiences = configuration.GetSection("HrkAudiences")
                         .Get<List<string>>() ?? new List<string>();


            NotEmpty(x => x.Audience)
           .Must(aud => allowedAudiences.Contains(aud))
           .WithMessage("Invalid audience.");

        }

    }
}
