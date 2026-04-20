using Microsoft.AspNetCore.Authentication;
using System.Net;
using System.Net.Http.Headers;

public sealed class TokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _envId;
    private readonly string _partnerBlock;

    public TokenHandler(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _envId = configuration["UserEnvironment:EnvId"]
                 ?? throw new InvalidOperationException("UserEnvironment:EnvId is not configured.");

        _partnerBlock = "eyJQYXJ0bmVyTmFtZSI6IlFOWFQgTU9ERVJOSVpBVElPTiBVSSIsIlBhcnRuZXJUaW1lIjoiMjAyNi0wNC0yMFQxMDozNzoxMi4zNzExNTAyLTA0OjAwIiwiUGFydG5lcklkIjoiei1vVlFLckY0ZS9sRXRYeFAxOEdyMmhVRVZUM1hZekFKMEFYdVFZWEtCMFlKUDYrK1E5NVVQOER4WVB0V2pEY1RReFhXYWhEYXBFUDYvVmhHanJBUFphTjJyaTFmWFBKMTVHMU9ncWVIcGRwRUczdmNlaVJTalhhRC8ycG44eTV5RTV4K3dMNFpKT0pwMUc3NDdISG9UelE9PSIsIkxNSWQiOiJ6LW9WUUtyRjRlL2xFdFh4UDE4R3IyaFVFVlQzWFl6QUowQVh1UVlYS0IwWUpQNisrUTk1VVA4RHhZUHRXakRjVFF4WFdhaERhcEVQNi9WaEdqckFQWmFOMnJpMWZYUEoxNUcxT2dxZUhwZHBFRzN2Y2VpUlNqWGFELzJwbjh5NXlFNXgrd0w0WkpPSnAxRzc0N0hIb1R6UT09IiwiTE1OYW1lIjoiUU5YVCBNT0RFUk5JWkFUSU9OIFVJIiwiTE1UaW1lIjoiMjAyNi0wNC0yMFQxMDozNzoxMi4zNzExNTAyLTA0OjAwIn0";
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var httpContext = _httpContextAccessor.HttpContext
                          ?? throw new InvalidOperationException("HttpContext is not available.");

        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);

        var accessToken = await httpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        request.Headers.TryAddWithoutValidation("X-TZ-EnvId", _envId);
        request.Headers.Add("X-TZ-Partner", _partnerBlock);

        return await base.SendAsync(request, ct);
    }
}