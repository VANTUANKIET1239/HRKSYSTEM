using GAME.Application.Features.Queries.Catalog;
using HRK.GAME.Common;
using Core.Application.CQRS;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Controllers
{
    [Route("api/catalog")]
    public class CatalogController : HRKControllerBase
    {
        private readonly ICqrsDispatcher _dispatcher;

        public CatalogController(ICqrsDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [HttpGet("heroes")]
        public async Task<IActionResult> GetHeroTemplates()
        {
            var response = await _dispatcher.QueryAsync(new GetHeroTemplatesQuery(),HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpGet("hero-detail")]
        public async Task<IActionResult> GetHeroTemplateById([FromQuery] int id)
        {
            var response = await _dispatcher.QueryAsync(new GetHeroTemplateByIdQuery(id),HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpGet("skills")]
        public async Task<IActionResult> GetSkillTemplates()
        {
            var response = await _dispatcher.QueryAsync(new GetSkillTemplatesQuery(),HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpGet("items")]
        public async Task<IActionResult> GetItemTemplates([FromQuery] int? categoryId, [FromQuery] int? rarityId)
        {
            var response = await _dispatcher.QueryAsync(new GetItemTemplatesQuery(categoryId, rarityId),HttpContext.RequestAborted);
            return HrkOk(response);
        }
    }
}
