using Core.Localization;
using Core.Web.Compression;
using Language.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace HRK.LANGUAGE.Controllers;

[ApiController]
[Route("api/language/bundles")]
public sealed class LanguageBundlesController(IPublishedLanguageReader reader) : ControllerBase
{
    [HttpGet("{group}/{culture}")]
    [HrkResponseCompression("Language")]
    public async Task<IActionResult> Get(string group, string culture, CancellationToken ct)
    {
        culture = TranslationIdentity.Culture(culture) ?? "";
        if (!TranslationIdentity.ValidGroup(group) || culture.Length == 0) return NotFound();
        var bundle = await reader.GetAsync(group, culture, ct);
        if (bundle is null) return NotFound();
        var etag = new EntityTagHeaderValue(FormattableString.Invariant($"\"{group}-{culture}-r{bundle.Revision}\""), isWeak: true);
        Response.GetTypedHeaders().ETag = etag;
        Response.Headers.CacheControl = "private, no-cache";
        Response.Headers.ContentLanguage = culture;
        IList<EntityTagHeaderValue>? conditions;
        try { conditions = Request.GetTypedHeaders().IfNoneMatch; }
        catch (FormatException) { return BadRequest(); }
        if (conditions?.Any(candidate => candidate == EntityTagHeaderValue.Any || candidate.Compare(etag, useStrongComparison: false)) == true)
            return StatusCode(StatusCodes.Status304NotModified);
        return Ok(bundle);
    }
}
