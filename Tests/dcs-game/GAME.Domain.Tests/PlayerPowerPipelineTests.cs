using Core.Application.CQRS;
using Core.Application.DependencyInjection;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.DependencyInjection;
using GAME.Application.DTOs;
using GAME.Application.Features.Queries.GameConfig;
using GAME.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAME.Domain.Tests;

public sealed class PlayerPowerPipelineTests
{
    [Fact]
    public async Task UnmarkedQueryDoesNotResolveRankingOrActorDependencies()
    {
        var services = new ServiceCollection();
        services.AddCoreApplication(typeof(IPlayerRankingWriter).Assembly)
            .AddPlayerPowerChangePipeline().AddPlayerPowerChangePipeline();
        services.AddSingleton<IRequestHandler<GetGameFeaturesQuery, BaseResponse<List<GameFeatureConfigDto>>>>(
            new PipelineTestHandler<GetGameFeaturesQuery, BaseResponse<List<GameFeatureConfigDto>>>(
                (_, _) => Task.FromResult(BaseResponse<List<GameFeatureConfigDto>>.SuccessResponse([]))));
        using var provider = services.BuildServiceProvider();
        var result = await provider.GetRequiredService<ICqrsDispatcher>().QueryAsync(new GetGameFeaturesQuery());
        Assert.Empty(result.Data!);
    }
}
