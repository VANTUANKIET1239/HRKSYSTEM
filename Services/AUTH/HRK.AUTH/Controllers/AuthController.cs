using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.RabbitMQ.Interfaces;
using HRK.AUTH.Common;
using AUTH.Application.Features.Commands.LoginUser;
using AUTH.Application.Features.Commands.RegisterUser;
using AUTH.Application.Features.Commands.RefreshToken;
using AUTH.Application.Features.Commands.LogoutUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using static HRK.AUTH.Configuration.DependencyInjection;

namespace HRK.AUTH.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : HRKControllerBase
    {
        private readonly IMediator _mediator;
      //  private readonly IMessagePublisher _messagePublisher;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
          //  this._messagePublisher = messagePublisher;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }


        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            LogouUserCommand command = new();
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


        [HttpPost("Test")]
        public async Task<IActionResult> Test()
        {

            var _defaultCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                IsEssential = true,
                

            };
            Response.Cookies.Append("Test","vantuankiet" , _defaultCookieOptions);

            var msg = new DemoMessage(Guid.NewGuid(), "kiet", DateTime.UtcNow);
           // await _messagePublisher.PublishJsonAsync(msg, ct: default);
            return Ok(new { status = "published", msg.Id });
        }
    }
}
