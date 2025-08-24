using HRK.AUTH.Common;
using HRK.AUTH.Features.Commands.LoginUser;
using MediatR;
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


        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginUserCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpGet("Test")]
        public  IActionResult Test()
        {
          //  var result = await _mediator.Send(command);
            return Ok("test");
        }
    }
}
