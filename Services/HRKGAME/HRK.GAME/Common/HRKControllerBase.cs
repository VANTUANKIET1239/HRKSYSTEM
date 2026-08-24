using Core.Common.Entity.MyCompany.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRK.GAME.Common
{
    [ApiController]
    [Produces("application/json")]
    public class HRKControllerBase : ControllerBase
    {
        protected string GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                         ?? User.FindFirstValue("sub") 
                         ?? User.FindFirstValue("UserId")
                         ?? User.FindFirstValue("id");

            return userId ?? string.Empty;
        }

        protected IActionResult OkResponse<T>(T data, string? message = null)
        {
            var response = BaseResponse<T>.SuccessResponse(data, message);
            return Ok(response);
        }

        protected IActionResult ErrorResponse(string errorMessage)
        {
            var response = BaseResponse<string>.FailResponse(errorMessage);
            return BadRequest(response);
        }

        protected IActionResult ErrorResponse(List<string> errors)
        {
            var response = BaseResponse<string>.FailResponse(errors);
            return BadRequest(response);
        }

        protected IActionResult NotFoundResponse(string message = "Resource not found.")
        {
            var response = BaseResponse<string>.FailResponse(message);
            return NotFound(response);
        }

        protected ObjectResult HrkOk<T>(BaseResponse<T> response)
        {
            if (!response.Success)
            {
                return StatusCode(response.StatusCode > 0 ? response.StatusCode : 400, response);
            }
            return Ok(response);
        }
    }
}
