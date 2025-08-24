using Core.Common.Entity.MyCompany.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace HRK.AUTH.Common
{
    [ApiController]
    [Produces("application/json")]
    public class HRKControllerBase : ControllerBase
    {
        /// <summary>
        /// Returns standardized success response.
        /// </summary>
        protected IActionResult OkResponse<T>(T data, string message = null)
        {
            var response = BaseResponse<T>.SuccessResponse(data, message);
            return Ok(response);
        }

        /// <summary>
        /// Returns standardized error response.
        /// </summary>
        protected IActionResult ErrorResponse(string errorMessage)
        {
            var response = BaseResponse<string>.FailResponse(errorMessage);
            return BadRequest(response);
        }

        /// <summary>
        /// Returns standardized error response with multiple errors.
        /// </summary>
        protected IActionResult ErrorResponse(List<string> errors)
        {
            var response = BaseResponse<string>.FailResponse(errors);
            return BadRequest(response);
        }

        /// <summary>
        /// Returns standardized not found response.
        /// </summary>
        protected IActionResult NotFoundResponse(string message = "Resource not found.")
        {
            var response = BaseResponse<string>.FailResponse(message);
            return NotFound(response);
        }   
    }
}
