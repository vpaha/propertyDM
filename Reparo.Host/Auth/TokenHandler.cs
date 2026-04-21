using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

public sealed class TokenHandlerOptions
{
    public string EnvId { get; set; } = string.Empty;
    public string PartnerBlock { get; set; } = string.Empty;
}

public class TokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TokenHandlerOptions _options;

    public TokenHandler(IHttpContextAccessor httpContextAccessor, IOptions<TokenHandlerOptions> options)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
        var accessToken = await httpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        request.Headers.TryAddWithoutValidation("X-TZ-EnvId", _options.EnvId);
        request.Headers.TryAddWithoutValidation("X-TZ-Partner", _options.PartnerBlock);
        return await base.SendAsync(request, cancellationToken);
    }
}