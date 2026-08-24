using GAME.Application.Features.Queries.Metadata;
using HRK.GAME.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Controllers
{
    [Route("api/metadata")]
    public class MetadataController : HRKControllerBase
    {
        private readonly IMediator _mediator;

        public MetadataController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("rarities")]
        public async Task<IActionResult> GetRarities()
        {
            var response = await _mediator.Send(new GetRaritiesQuery());
            return HrkOk(response);
        }

        [HttpGet("factions")]
        public async Task<IActionResult> GetFactions()
        {
            var response = await _mediator.Send(new GetFactionsQuery());
            return HrkOk(response);
        }

        [HttpGet("classes")]
        public async Task<IActionResult> GetClasses()
        {
            var response = await _mediator.Send(new GetClassesQuery());
            return HrkOk(response);
        }

        [HttpGet("item-categories")]
        public async Task<IActionResult> GetItemCategories()
        {
            var response = await _mediator.Send(new GetItemCategoriesQuery());
            return HrkOk(response);
        }

        [HttpGet("skill-enums")]
        public async Task<IActionResult> GetSkillEnums()
        {
            var response = await _mediator.Send(new GetSkillEnumsQuery());
            return HrkOk(response);
        }
    }
}
