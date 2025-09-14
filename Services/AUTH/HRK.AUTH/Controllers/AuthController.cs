using HRK.AUTH.Common;
using HRK.AUTH.Features.Commands.LoginUser;
using HRK.AUTH.Features.Commands.RefreshToken;
using Core.Common.Entity.MyCompany.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HRK.AUTH.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : HRKControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
        {
            var response = await _mediator.Send(command);

            if (!response.Success)
            {
                return StatusCode(response.StatusCode, response);
            }

            return Ok(response);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
        {
            var response = await _mediator.Send(command);

            return Ok(response);
        }


        [HttpGet("Test")]
        [Authorize]
        public  IActionResult Test()
        {
            return Ok("test");
        }
    }
}
