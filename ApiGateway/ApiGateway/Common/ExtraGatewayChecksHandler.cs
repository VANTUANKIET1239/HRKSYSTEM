namespace ApiGateway.Common
{
    public class ExtraGatewayChecksHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ExtraGatewayChecksHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var httpCtx = _httpContextAccessor.HttpContext!;
            var user = httpCtx.User;

            // Example: block revoked tokens by jti blacklist
            var jti = user.FindFirst("jti")?.Value;
            if (jti is null || await IsRevokedAsync(jti, ct))
            {
                var resp = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
                resp.Content = new StringContent("Token revoked or missing jti.");
                return resp; // stop here, never reaches downstream
            }

            // Pass through
            return await base.SendAsync(request, ct);
        }

        private Task<bool> IsRevokedAsync(string jti, CancellationToken ct)
        {
            // TODO: check Redis/DB for revoked JTI
            return Task.FromResult(false);
        }
    }
}
