using GAME.Application.Features.Queries.Player;
using HRK.GAME.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace HRK.GAME.Controllers
{
    [Authorize]
    [Route("api/player")]
    public class PlayerController : HRKControllerBase
    {
        private readonly IMediator _mediator;

        public PlayerController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetGameInfo()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var service = HttpContext.RequestServices.GetRequiredService<global::GAME.Application.Interfaces.IGamePlayerService>();
            var data = await service.GetPlayerGameInfoAsync(userId);
            return HrkOk(Core.Common.Entity.MyCompany.Shared.Responses.BaseResponse<global::GAME.Application.DTOs.PlayerGameInfoDto?>.SuccessResponse(data));
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var response = await _mediator.Send(new GetPlayerProfileQuery());
            return HrkOk(response);
        }

        [HttpGet("wallet")]
        public async Task<IActionResult> GetWallet()
        {
            var response = await _mediator.Send(new GetPlayerWalletQuery());
            return HrkOk(response);
        }

        [HttpGet("avatars")]
        public async Task<IActionResult> GetAvatars([FromServices] IGamePlayerService service)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            return OkResponse(await service.GetAvatarTemplatesAsync(userId, HttpContext.RequestAborted));
        }

        [HttpPut("avatar/template")]
        public async Task<IActionResult> SelectAvatar([FromBody] SelectAvatarRequest request, [FromServices] IGamePlayerService service)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            try { return OkResponse(await service.SelectAvatarTemplateAsync(userId, request.AvatarTemplateId, HttpContext.RequestAborted)); }
            catch (KeyNotFoundException ex) { return NotFoundResponse(ex.Message); }
        }

        [HttpPut("avatar/custom")]
        [RequestSizeLimit(1_100_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 1_100_000)]
        public async Task<IActionResult> UploadCustomAvatar(IFormFile file, [FromServices] IGamePlayerService service)
        {
            const int maxBytes = 1_048_576;
            const int maxDimension = 1024;
            if (file == null || file.Length == 0) return ErrorResponse("Vui lòng chọn một ảnh để tải lên.");
            if (file.Length > maxBytes) return ErrorResponse("Ảnh vượt quá dung lượng tối đa 1 MB.");

            await using var stream = file.OpenReadStream();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, HttpContext.RequestAborted);
            var bytes = memory.ToArray();
            if (!TryReadImage(bytes, out var contentType, out var width, out var height))
                return ErrorResponse("File không phải ảnh JPEG, PNG hoặc WebP hợp lệ.");
            if (width > maxDimension || height > maxDimension || (long)width * height > maxDimension * maxDimension)
                return ErrorResponse($"Ảnh có độ phân giải {width}×{height}, vượt giới hạn 1024×1024.");

            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var profile = await service.SaveCustomAvatarAsync(userId, new CustomAvatarUploadDto
            {
                ImageData = bytes, ContentType = contentType, FileName = Path.GetFileName(file.FileName), Width = width, Height = height
            }, HttpContext.RequestAborted);
            return OkResponse(profile, "Cập nhật ảnh đại diện thành công.");
        }

        [AllowAnonymous]
        [HttpGet("avatar/custom/{playerId:long}")]
        public async Task<IActionResult> GetCustomAvatar(long playerId, [FromServices] IGamePlayerService service)
        {
            var avatar = await service.GetCustomAvatarAsync(playerId, HttpContext.RequestAborted);
            if (avatar == null) return NotFound();
            Response.Headers.ETag = $"\"{avatar.ContentHash}\"";
            Response.Headers.CacheControl = "public,max-age=3600";
            return File(avatar.ImageData, avatar.ContentType);
        }

        [HttpDelete("avatar/custom")]
        public async Task<IActionResult> DeleteCustomAvatar([FromServices] IGamePlayerService service)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            return OkResponse(await service.DeleteCustomAvatarAsync(userId, HttpContext.RequestAborted));
        }

        private static bool TryReadImage(byte[] data, out string contentType, out int width, out int height)
        {
            contentType = string.Empty; width = 0; height = 0;
            if (data.Length >= 24 && data.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            {
                contentType = "image/png";
                width = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(16, 4));
                height = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(20, 4));
                return width > 0 && height > 0;
            }
            if (data.Length >= 12 && data[0] == 0xFF && data[1] == 0xD8)
            {
                var offset = 2;
                while (offset + 9 < data.Length)
                {
                    if (data[offset] != 0xFF) { offset++; continue; }
                    var marker = data[offset + 1];
                    if (marker is 0xD8 or 0xD9) { offset += 2; continue; }
                    if (offset + 4 > data.Length) break;
                    var length = (data[offset + 2] << 8) | data[offset + 3];
                    if (length < 2 || offset + 2 + length > data.Length) break;
                    if (marker is >= 0xC0 and <= 0xC3 or >= 0xC5 and <= 0xC7 or >= 0xC9 and <= 0xCB or >= 0xCD and <= 0xCF)
                    {
                        height = (data[offset + 5] << 8) | data[offset + 6];
                        width = (data[offset + 7] << 8) | data[offset + 8];
                        contentType = "image/jpeg";
                        return width > 0 && height > 0;
                    }
                    offset += 2 + length;
                }
            }
            if (data.Length >= 30 && System.Text.Encoding.ASCII.GetString(data, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(data, 8, 4) == "WEBP")
            {
                var chunk = System.Text.Encoding.ASCII.GetString(data, 12, 4);
                if (chunk == "VP8X")
                {
                    width = 1 + data[24] + (data[25] << 8) + (data[26] << 16);
                    height = 1 + data[27] + (data[28] << 8) + (data[29] << 16);
                    contentType = "image/webp";
                    return width > 0 && height > 0;
                }
                if (chunk == "VP8 " && data.Length >= 30 && data[23] == 0x9D && data[24] == 0x01 && data[25] == 0x2A)
                {
                    width = (data[26] | data[27] << 8) & 0x3FFF;
                    height = (data[28] | data[29] << 8) & 0x3FFF;
                    contentType = "image/webp";
                    return width > 0 && height > 0;
                }
                if (chunk == "VP8L" && data.Length >= 25 && data[20] == 0x2F)
                {
                    width = 1 + data[21] + ((data[22] & 0x3F) << 8);
                    height = 1 + ((data[22] >> 6) | (data[23] << 2) | ((data[24] & 0x0F) << 10));
                    contentType = "image/webp";
                    return width > 0 && height > 0;
                }
            }
            return false;
        }

        public sealed record SelectAvatarRequest(int AvatarTemplateId);
    }
}
