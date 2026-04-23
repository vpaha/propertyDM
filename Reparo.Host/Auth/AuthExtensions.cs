using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

public static class AuthExtensions
{
    public static IServiceCollection AddHostAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var appBasePath = configuration["AppBasePath"] ?? "/";

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignOutScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Path = appBasePath;
                options.Cookie.Name = ".Pavel.Auth";
                options.AccessDeniedPath = "/accessdenied";
            })
            .AddGoogle(options =>
            {
                var google = configuration.GetSection("UserEnvironment:Google");
                options.ClientId = google["Client_Id"] ?? string.Empty;

                var clientSecret = google["ClientSecret"];
                if (!string.IsNullOrWhiteSpace(clientSecret))
                    options.ClientSecret = clientSecret;

                options.Scope.Add("email");
                options.Scope.Add("profile");
                options.SaveTokens = true;
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                options.Events = new OAuthEvents
                {
                    OnCreatingTicket = async ctx =>
                    {
                        var principal = ctx.Principal;
                        if (principal is null)
                            throw new InvalidOperationException("Principal is not available.");

                        await ProvisionUserAsync(ctx.HttpContext, principal, ctx.HttpContext.RequestAborted);
                    }
                };
            })
            .AddTwitter(options =>
            {
                var twitter = configuration.GetSection("UserEnvironment:Twitter");
                options.ConsumerKey = twitter["ConsumerAPIKey"];
                options.ConsumerSecret = twitter["ConsumerSecret"];
                options.CallbackPath = "/signin-twitter";
                options.SaveTokens = true;
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                options.Events = new TwitterEvents
                {
                    OnCreatingTicket = async ctx =>
                    {
                        var principal = ctx.Principal;
                        if (principal is null)
                            throw new InvalidOperationException("Principal is not available.");

                        await ProvisionUserAsync(ctx.HttpContext, principal, ctx.HttpContext.RequestAborted);
                    }
                };
            })
            .AddFacebook(options =>
            {
                var facebook = configuration.GetSection("UserEnvironment:Facebook");
                options.AppId = facebook["AppId"] ?? string.Empty;

                var appSecret = facebook["AppSecret"];
                if (!string.IsNullOrWhiteSpace(appSecret))
                    options.AppSecret = appSecret;

                options.AccessDeniedPath = "/accessdenied";
                options.CallbackPath = "/signin-facebook";
                options.SaveTokens = true;
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                options.Events = new OAuthEvents
                {
                    OnCreatingTicket = async ctx =>
                    {
                        var principal = ctx.Principal;
                        if (principal is null)
                            throw new InvalidOperationException("Principal is not available.");

                        await ProvisionUserAsync(ctx.HttpContext, principal, ctx.HttpContext.RequestAborted);
                    }
                };
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                var oidc = configuration.GetSection("UserEnvironment:OidcClientOptions");

                options.Authority = oidc["Authority"] ?? string.Empty;
                options.ClientId = oidc["Client_Id"] ?? string.Empty;

                var clientSecret = oidc["ClientSecret"];
                if (!string.IsNullOrWhiteSpace(clientSecret))
                    options.ClientSecret = clientSecret;

                var extraScope = oidc["Scope"];
                AddScopes(options, extraScope);

                options.ResponseType = "code";
                options.AccessDeniedPath = "/accessdenied";
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
                        var principal = ctx.Principal;
                        if (principal is null)
                            throw new InvalidOperationException("Principal is not available.");

                        await ProvisionUserAsync(ctx.HttpContext, principal, ctx.HttpContext.RequestAborted);
                    }
                };
            });

        return services;
    }

    private static void AddScopes(OpenIdConnectOptions options, string? extraScopes, params string[] defaults)
    {
        ArgumentNullException.ThrowIfNull(options);

        var scopes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var scope in defaults) if (!string.IsNullOrWhiteSpace(scope)) scopes.Add(scope);

        if (!string.IsNullOrWhiteSpace(extraScopes))
        {
            foreach (var scope in extraScopes.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                scopes.Add(scope);
        }

        foreach (var scope in scopes) options.Scope.Add(scope);
    }

    private static async Task ProvisionUserAsync(
        HttpContext httpContext,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var scopeFactory = httpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();
        await using var scope = scopeFactory.CreateAsyncScope();

        var provisioner = scope.ServiceProvider.GetRequiredService<IUserService>();
        await provisioner.ProvisionAsync(principal, cancellationToken);
    }
}