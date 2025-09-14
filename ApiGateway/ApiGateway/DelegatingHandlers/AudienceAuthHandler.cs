using System.Net;

namespace ApiGateway.DelegatingHandlers
{
    //public class AudienceAuthHandler : DelegatingHandler
    //{
    //    private readonly ITokenClient _tokens; // wraps /auth/refresh
    //    private readonly string _audience;

    //    public AudienceAuthHandler(ITokenClient tokens, string audience)
    //    { _tokens = tokens; _audience = audience; }

    //    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    //    {
    //        req.Headers.Authorization = new("Bearer", await _tokens.GetAccessTokenAsync(_audience));
    //        var res = await base.SendAsync(req, ct);
    //        if (res.StatusCode == HttpStatusCode.Unauthorized)
    //        {
    //            // force refresh & retry once
    //            req.Headers.Authorization = new("Bearer", await _tokens.GetAccessTokenAsync(_audience, force: true));
    //            res.Dispose();
    //            return await base.SendAsync(req, ct);
    //        }
    //        return res;
    //    }
    //}
}
