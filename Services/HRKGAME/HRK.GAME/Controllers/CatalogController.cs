using GAME.Application.Features.Queries.Catalog;
using HRK.GAME.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Controllers
{
    [Route("api/catalog")]
    public class CatalogController : HRKControllerBase
    {
        private readonly IMediator _mediator;

        public CatalogController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("heroes")]
        public async Task<IActionResult> GetHeroTemplates()
        {
            var response = await _mediator.Send(new GetHeroTemplatesQuery());
            return HrkOk(response);
        }

        [HttpGet("hero-detail")]
        public async Task<IActionResult> GetHeroTemplateById([FromQuery] int id)
        {
            var response = await _mediator.Send(new GetHeroTemplateByIdQuery(id));
            return HrkOk(response);
        }

        [HttpGet("skills")]
        public async Task<IActionResult> GetSkillTemplates()
        {
            var response = await _mediator.Send(new GetSkillTemplatesQuery());
            return HrkOk(response);
        }

        [HttpGet("items")]
        public async Task<IActionResult> GetItemTemplates([FromQuery] int? categoryId, [FromQuery] int? rarityId)
        {
            var response = await _mediator.Send(new GetItemTemplatesQuery(categoryId, rarityId));
            return HrkOk(response);
        }
    }
}
