using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

public static class AuthExtensions
{
    private const string AccessDeniedPath = "/accessdenied";
    private const string CookieName = ".Pavel.Auth";

    public static IServiceCollection AddHostAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var appBasePath = configuration["AppBasePath"] ?? "/";

        var auth = services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignOutScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        });

        auth.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Cookie.Path = appBasePath;
            options.Cookie.Name = CookieName;
            options.AccessDeniedPath = AccessDeniedPath;
        });

        AddGoogleIfConfigured(auth, configuration);
        AddTwitterIfConfigured(auth, configuration);
        AddFacebookIfConfigured(auth, configuration);
        AddOpenIdConnectIfConfigured(auth, configuration);

        return services;
    }

    private static void AddGoogleIfConfigured(
        AuthenticationBuilder auth,
        IConfiguration configuration)
    {
        var google = configuration.GetSection("UserEnvironment:Google");

        var clientId = google["Client_Id"];
        var clientSecret = google["ClientSecret"];

        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret))
            return;

        auth.AddGoogle(options =>
        {
            options.ClientId = clientId;
            options.ClientSecret = clientSecret;

            options.Scope.Add("email");
            options.Scope.Add("profile");
            options.SaveTokens = true;
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

            options.Events = new OAuthEvents
            {
                OnCreatingTicket = async ctx =>
                {
                    var principal = ctx.Principal
                        ?? throw new InvalidOperationException("Principal is not available.");

                    await ProvisionAsync(
                        ctx.HttpContext,
                        principal,
                        ctx.HttpContext.RequestAborted);
                }
            };
        });
    }

    private static void AddTwitterIfConfigured(
        AuthenticationBuilder auth,
        IConfiguration configuration)
    {
        var twitter = configuration.GetSection("UserEnvironment:Twitter");

        var consumerKey = twitter["ConsumerAPIKey"];
        var consumerSecret = twitter["ConsumerSecret"];

        if (string.IsNullOrWhiteSpace(consumerKey) ||
            string.IsNullOrWhiteSpace(consumerSecret))
            return;

        auth.AddTwitter(options =>
        {
            options.ConsumerKey = consumerKey;
            options.ConsumerSecret = consumerSecret;
            options.CallbackPath = "/signin-twitter";
            options.SaveTokens = true;
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

            options.Events = new TwitterEvents
            {
                OnCreatingTicket = async ctx =>
                {
                    var principal = ctx.Principal
                        ?? throw new InvalidOperationException("Principal is not available.");

                    await ProvisionAsync(
                        ctx.HttpContext,
                        principal,
                        ctx.HttpContext.RequestAborted);
                }
            };
        });
    }

    private static void AddFacebookIfConfigured(
        AuthenticationBuilder auth,
        IConfiguration configuration)
    {
        var facebook = configuration.GetSection("UserEnvironment:Facebook");

        var appId = facebook["AppId"];
        var appSecret = facebook["AppSecret"];

        if (string.IsNullOrWhiteSpace(appId) ||
            string.IsNullOrWhiteSpace(appSecret))
            return;

        auth.AddFacebook(options =>
        {
            options.AppId = appId;
            options.AppSecret = appSecret;

            options.AccessDeniedPath = AccessDeniedPath;
            options.CallbackPath = "/signin-facebook";
            options.SaveTokens = true;
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

            options.Events = new OAuthEvents
            {
                OnCreatingTicket = async ctx =>
                {
                    var principal = ctx.Principal
                        ?? throw new InvalidOperationException("Principal is not available.");

                    await ProvisionAsync(
                        ctx.HttpContext,
                        principal,
                        ctx.HttpContext.RequestAborted);
                }
            };
        });
    }

    private static void AddOpenIdConnectIfConfigured(
        AuthenticationBuilder auth,
        IConfiguration configuration)
    {
        var oidc = configuration.GetSection("UserEnvironment:OidcClientOptions");

        var authority = oidc["Authority"];
        var clientId = oidc["Client_Id"];

        if (string.IsNullOrWhiteSpace(authority) ||
            string.IsNullOrWhiteSpace(clientId))
            return;

        auth.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Authority = authority;
            options.ClientId = clientId;

            var clientSecret = oidc["ClientSecret"];
            if (!string.IsNullOrWhiteSpace(clientSecret))
                options.ClientSecret = clientSecret;

            AddScopes(options, oidc["Scope"]);

            options.ResponseType = "code";
            options.AccessDeniedPath = AccessDeniedPath;
            options.CallbackPath = "/signin-oidc";
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.SaveTokens = true;
            options.MapInboundClaims = false;
            options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = "name",
                RoleClaimType = "role"
            };

            options.Events = new OpenIdConnectEvents
            {
                OnTokenValidated = async ctx =>
                {
                    var principal = ctx.Principal
                        ?? throw new InvalidOperationException("Principal is not available.");

                    await ProvisionAsync(
                        ctx.HttpContext,
                        principal,
                        ctx.HttpContext.RequestAborted);
                }
            };
        });
    }

    private static void AddScopes(
        OpenIdConnectOptions options,
        string? extraScopes,
        params string[] defaults)
    {
        ArgumentNullException.ThrowIfNull(options);

        var scopes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var scope in defaults)
        {
            if (!string.IsNullOrWhiteSpace(scope))
                scopes.Add(scope);
        }

        if (!string.IsNullOrWhiteSpace(extraScopes))
        {
            foreach (var scope in extraScopes.Split(
                         [' ', ','],
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                scopes.Add(scope);
            }
        }

        foreach (var scope in scopes)
            options.Scope.Add(scope);
    }

    private static async Task ProvisionAsync(
        HttpContext httpContext,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var scopeFactory = httpContext.RequestServices
            .GetRequiredService<IServiceScopeFactory>();

        await using var scope = scopeFactory.CreateAsyncScope();

        var provisioner = scope.ServiceProvider.GetRequiredService<IUserService>();

        await provisioner.ProvisionAsync(principal, cancellationToken);
    }
}

public static class PrincipalExtensions
{
    public static int? GetVendorId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst("vendor_id")?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }

    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst("app_user_id")?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }
}