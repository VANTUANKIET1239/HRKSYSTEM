using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Features.Commands.Inventory.EnhanceEquipment
{
    public record EnhanceEquipmentCommand(EnhanceEquipmentRequestDto Request) : ICommand<BaseResponse<EnhanceEquipmentResultDto>>;

    public class EnhanceEquipmentCommandHandler : HRKBaseCommand, ICommandHandler<EnhanceEquipmentCommand, BaseResponse<EnhanceEquipmentResultDto>>
    {
        private readonly IEquipmentEnhancementService _enhancementService;
        private readonly ILogger<EnhanceEquipmentCommandHandler> _logger;

        public EnhanceEquipmentCommandHandler(
            IEquipmentEnhancementService enhancementService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<EnhanceEquipmentCommandHandler> logger)
            : base(httpContextAccessor)
        {
            _enhancementService = enhancementService;
            _logger = logger;
        }

        public async Task<BaseResponse<EnhanceEquipmentResultDto>> Handle(EnhanceEquipmentCommand command, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var req = command.Request;

            _logger.LogInformation("EnhancementAttemptStarted: UserId={UserId}, RequestId={RequestId}, InventoryItemId={InventoryItemId}, StonesCount={StonesCount}, HasCharm={HasCharm}",
                userId, req.RequestId, req.InventoryItemId, req.StoneInventoryItemIds?.Count ?? 0, req.CharmInventoryItemId.HasValue);

            try
            {
                var result = await _enhancementService.EnhanceEquipmentAsync(userId, req, cancellationToken);

                if (result.Success)
                {
                    _logger.LogInformation("EnhancementAttemptSucceeded: UserId={UserId}, RequestId={RequestId}, InventoryItemId={InventoryItemId}, OldLevel={OldLevel}, NewLevel={NewLevel}, FinalRate={FinalRate:P2}",
                        userId, req.RequestId, req.InventoryItemId, result.OldEnhancement, result.NewEnhancement, result.FinalSuccessRate);

                    return BaseResponse<EnhanceEquipmentResultDto>.SuccessResponse(result, "Cường hóa trang bị thành công!");
                }
                else
                {
                    _logger.LogInformation("EnhancementAttemptFailed: UserId={UserId}, RequestId={RequestId}, InventoryItemId={InventoryItemId}, OldLevel={OldLevel}, NewLevel={NewLevel}, WasProtected={WasProtected}, FinalRate={FinalRate:P2}",
                        userId, req.RequestId, req.InventoryItemId, result.OldEnhancement, result.NewEnhancement, result.WasLevelProtected, result.FinalSuccessRate);

                    return BaseResponse<EnhanceEquipmentResultDto>.SuccessResponse(result, result.WasLevelProtected ? "Cường hóa thất bại, Bùa Hộ Mệnh đã bảo vệ cấp độ!" : "Cường hóa thất bại!");
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("EnhancementAttemptRejected: UserId={UserId}, RequestId={RequestId}, Reason={Reason}",
                    userId, req.RequestId, ex.Message);
                return BaseResponse<EnhanceEquipmentResultDto>.FailResponse(ex.Message, statusCode: 404);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("EnhancementAttemptRejected: UserId={UserId}, RequestId={RequestId}, Reason={Reason}",
                    userId, req.RequestId, ex.Message);
                return BaseResponse<EnhanceEquipmentResultDto>.FailResponse(ex.Message, statusCode: 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EnhancementAttemptError: UserId={UserId}, RequestId={RequestId}, Error={Error}",
                    userId, req.RequestId, ex.Message);
                return BaseResponse<EnhanceEquipmentResultDto>.FailResponse("Đã xảy ra lỗi trong quá trình cường hóa trang bị: " + ex.Message, statusCode: 500);
            }
        }
    }
}
