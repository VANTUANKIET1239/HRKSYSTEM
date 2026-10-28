using Core.Common.Errors;
using GAME.Application.DTOs;
using GAME.Application.Services;
using Xunit;

namespace GAME.Domain.Tests;

public sealed class FormationPreviewErrorTests
{
    [Fact]
    public async Task UnknownRequestedFormation_PropagatesAsNotFoundInsteadOfBecomingAPreviewSuccess()
    {
        // Missing template is detected before any stat/power dependency is used.
        var service = new FormationSnapshotService(new TowerTestStore(), null!, null!, null!);
        var error = await Assert.ThrowsAsync<BusinessException>(() => service.PreviewAsync(1, "MISSING",
            [new FormationPositionRequestDto { Slot = 1 }], default));
        Assert.Equal("FORMATION_NOT_FOUND", error.Code);
        Assert.Equal(BusinessErrorKind.NotFound, error.Kind);
        Assert.Equal("Không tìm thấy cấu hình trận pháp '{{p0}}'.", error.MessageTemplate);
        Assert.Equal("MISSING", error.Arguments!["p0"]);
    }

    [Fact]
    public async Task MissingDefaultConfiguration_RemainsAnInternalFailure()
    {
        var store = new TowerTestStore();
        store.Data<GAME.Domain.Entities.HrkFormationTemplate>().Clear();
        var service = new FormationSnapshotService(store, null!, null!, null!);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.BuildAsync(1, null,
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
