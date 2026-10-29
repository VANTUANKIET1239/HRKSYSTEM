using GAME.Application.Features.Queries.Metadata;
using HRK.GAME.Common;
using Core.Application.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Controllers
{
    [Route("api/metadata")]
    public class MetadataController : HRKControllerBase
    {
        private readonly ICqrsDispatcher _dispatcher;

        public MetadataController(ICqrsDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [HttpGet("rarities")]
        public async Task<IActionResult> GetRarities()
        {
            var response = await _dispatcher.QueryAsync(new GetRaritiesQuery(),HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpGet("factions")]
        public async Task<IActionResult> GetFactions()
        {
            var response = await _dispatcher.QueryAsync(new GetFactionsQuery(),HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpGet("classes")]
        public async Task<IActionResult> GetClasses()
        {
            var response = await _dispatcher.QueryAsync(new GetClassesQuery(),HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpGet("item-categories")]
        public async Task<IActionResult> GetItemCategories()
        {
            var response = await _dispatcher.QueryAsync(new GetItemCategoriesQuery(),HttpContext.RequestAborted);
            return HrkOk(response);
        }

        [HttpGet("skill-enums")]
        public async Task<IActionResult> GetSkillEnums()
        {
            var response = await _dispatcher.QueryAsync(new GetSkillEnumsQuery(),HttpContext.RequestAborted);
            return HrkOk(response);
        }
    }
}
