using Azure;
using Core.Common.Entity.MyCompany.Shared.Responses;
using HRK.AUTH.Common;
using HRK.AUTH.Features.Commands.LoginUser;
using HRK.AUTH.Features.Commands.RefreshToken;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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


        [HttpPost("logout")]
        public async Task<IActionResult> Logout(LogouUserCommand command)
        {
            var response = await _mediator.Send(command);


            return HrkOk(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
        {
            var response = await _mediator.Send(command);

            return HrkOk(response);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
        {
            var response = await _mediator.Send(command);


            return HrkOk(response);
        }


        [HttpGet("Test")]
        public  IActionResult Test()
        {
            return Ok("test");
        }
    }
}
