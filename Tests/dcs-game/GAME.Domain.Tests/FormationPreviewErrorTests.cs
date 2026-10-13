using GAME.Application.DTOs;
using GAME.Application.Services;
using Xunit;

namespace GAME.Domain.Tests;

public sealed class FormationPreviewErrorTests
{
    [Fact]
    public async Task MissingConfiguration_PropagatesInsteadOfBecomingAUserValidationMessage()
    {
        // Missing template is detected before any stat/power dependency is used.
        var service = new FormationSnapshotService(new TowerTestStore(), null!, null!, null!);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.PreviewAsync(1, "MISSING",
            [new FormationPositionRequestDto { Slot = 1 }], default));
    }

    [Fact]
    public async Task InvalidDraft_IsStillAValidPreviewResult()
    {
        var service = new FormationSnapshotService(new TowerTestStore(), null!, null!, null!);
        var preview = await service.PreviewAsync(1, "LUC_DO",
            [new FormationPositionRequestDto { Slot = 6 }], default);
        Assert.False(preview.IsValid);
        Assert.NotEmpty(preview.ValidationErrors);
    }
}
