using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Net.Http.Headers;
using Stripe;
using Stripe.Checkout;
using System.Globalization;

internal static class RouteExtensions
{
    public sealed record ThemeCookieDto(string ThemeName, bool IsDarkMode, string Culture);

    private static bool IsAllowedProvider(string provider) =>
        string.Equals(provider, OpenIdConnectDefaults.AuthenticationScheme, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(provider, "Google", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(provider, "Twitter", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(provider, "Facebook", StringComparison.OrdinalIgnoreCase);

    private static LocationDto parseResponse(GeocodeResult response)
    {
        string? country = null;
        string? state = null;
        string? city = null;

        foreach (var component in response.AddressComponents)
        {
            if (component.Types == null) continue;

            if (component.Types.Contains("country")) country = component.ShortName;
            if (component.Types.Contains("administrative_area_level_1")) state = component.ShortName;
            if (component.Types.Contains("locality")) city = component.LongName;
        }

        return new LocationDto
        {
            Address = response.FormattedAddress,
            PlaceId = response.PlaceId,
            Latitude = response.Geometry?.Location?.Latitude ?? 0,
            Longitude = response.Geometry?.Location?.Longitude ?? 0,
            Placename = city + ", " + state,
            Region = (country != null && state != null) ? $"{country}-{state}" : null
        };
    }

    internal static IEndpointConventionBuilder MapLoginAndLogout(this RouteGroupBuilder group)
    {
        group.MapPost("signin", (HttpContext context,
            [FromForm] string provider,
            [FromForm] string returnUrl,
            [FromForm] bool? rememberMe) =>
        {
            if (!IsAllowedProvider(provider)) return Results.BadRequest("Invalid provider.");

            var properties = new AuthenticationProperties { RedirectUri = returnUrl };
            if (rememberMe == true)
            {
                properties.AllowRefresh = true;
                properties.IsPersistent = true;
                properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);
            }
            return TypedResults.Challenge(properties, [provider]);
        });

        group.MapGet("signout", (HttpContext context) =>
        {
            var basePath = context.Request.PathBase.HasValue ? context.Request.PathBase.Value : "/";
            var properties = new AuthenticationProperties { RedirectUri = basePath };
            return Results.SignOut(properties, [CookieAuthenticationDefaults.AuthenticationScheme]);
        });

        group.MapGet("clear", async (HttpContext context, [FromServices] IDamageService repo,
        IOutputCacheStore outputCacheStore, CancellationToken ct) =>
        {
            repo.InvalidateCache();

            await outputCacheStore.EvictByTagAsync("damage-sections", ct);

            context.Response.Headers.Append("Clear-Site-Data", "\"*\"");
            var basePath = context.Request.PathBase.HasValue ? context.Request.PathBase.Value : "/";
            return Results.Redirect(basePath);
        });
        return group;
    }

    internal static IEndpointConventionBuilder MapGoogleMapEndpoints(this RouteGroupBuilder group, string googleAPIKey)
    {
        group.MapGet("geocode/reverse", async (double lat, double lng, IHttpClientFactory httpClientFactory) =>
        {
            if (string.IsNullOrWhiteSpace(googleAPIKey)) return Results.Problem("GoogleMaps:ApiKey is not configured.", statusCode: 500);

            var latStr = lat.ToString(CultureInfo.InvariantCulture);
            var lngStr = lng.ToString(CultureInfo.InvariantCulture);

            var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latStr},{lngStr}&key={googleAPIKey}";

            var client = httpClientFactory.CreateClient();
            using var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return Results.Problem("Geocoding request failed.", statusCode: (int)resp.StatusCode);

            var data = await resp.Content.ReadFromJsonAsync<GeocodeResponse>();
            var response = data?.Results.FirstOrDefault();

            if (response != null) return Results.Ok(parseResponse(response));
            else return Results.NotFound("No geocoding results found.");
        });
        group.MapGet("geocode/lookup", async (string address, IHttpClientFactory httpClientFactory) =>
        {
            if (string.IsNullOrWhiteSpace(googleAPIKey)) return Results.Problem("GoogleMaps:ApiKey is not configured.", statusCode: 500);

            var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={address}&key={googleAPIKey}";

            var client = httpClientFactory.CreateClient();
            using var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return Results.Problem("Geocoding request failed.", statusCode: (int)resp.StatusCode);

            var data = await resp.Content.ReadFromJsonAsync<GeocodeResponse>();
            var response = data?.Results.FirstOrDefault();

            if (response != null) return Results.Ok(parseResponse(response));
            else return Results.NotFound("No geocoding results found.");
        });
        return group;
    }

    internal static IEndpointConventionBuilder MapUserSettingsEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("theme", async (HttpContext ctx) =>
        {
            var dto = await ctx.Request.ReadFromJsonAsync<ThemeCookieDto>();
            var themeName = string.IsNullOrWhiteSpace(dto?.ThemeName) ? "custom1" : dto!.ThemeName.Trim();
            themeName = new string(themeName.Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_').ToArray());
            if (string.IsNullOrWhiteSpace(themeName)) themeName = "custom1";

            var dark = dto!.IsDarkMode ? "1" : "0";
            var value = $"{themeName}|{dark}";

            ctx.Response.Cookies.Append("theme", value, new CookieOptions
            {
                HttpOnly = false,
                Secure = ctx.Request.IsHttps,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                Path = "/", // IMPORTANT: applies to sub apps as well
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });

            return Results.Ok(new { ok = true });
        });

        group.MapPost("culture", async (HttpContext ctx) =>
        {
            var dto = await ctx.Request.ReadFromJsonAsync<ThemeCookieDto>();
            var cultureCode = dto?.Culture;
            if (string.IsNullOrWhiteSpace(cultureCode)) cultureCode = new CultureData().DefaultCulture.Name;

            var requestCulture = new RequestCulture(cultureCode);
            ctx.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(requestCulture),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    HttpOnly = false,
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                    Secure = ctx.Request.IsHttps,
                    Path = "/"
                });

            return Results.Ok(new { ok = true });
        });

        return group;
    }

    internal static IEndpointConventionBuilder MapDamageEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("damage-sections", async ([FromServices] IDamageService repo, CancellationToken ct) =>
        {
            var sections = await repo.ListSectionTypesAsync(ct);
            return Results.Ok(sections);
        })
        .CacheOutput("DamageSections")
        .AddEndpointFilter(async (ctx, next) =>
        {
            var result = await next(ctx);
            var http = ctx.HttpContext;
            if ((HttpMethods.IsGet(http.Request.Method) || HttpMethods.IsHead(http.Request.Method)) && http.Response.StatusCode is >= 200 and < 300)
            {
                http.Response.Headers[HeaderNames.CacheControl] = "public, max-age=43200, s-maxage=43200, stale-while-revalidate=60";
                http.Response.Headers[HeaderNames.Vary] = "Accept-Encoding";
            }
            return result;
        });

        group.MapGet("damage-entries", async (HttpContext http, [FromServices] IDamageService repo, [FromQuery] bool isVendor, CancellationToken ct) =>
        {
            int? userId = null;
            int? vendorId = null;

            if (isVendor) vendorId = http.User.GetVendorId();
            if (vendorId is null) userId = http.User.GetUserId();

            var entries = await repo.ListDamageUserEntriesAsync(userId, vendorId, ct);
            return Results.Ok(entries);
        });

        group.MapPost("damage-add", async (HttpContext http, [FromServices] IDamageService repo, [FromBody] DamageEntry entry, CancellationToken ct) =>
        {
            var userId = http.User.GetUserId();
            if (userId is not null) entry.UserId = userId.Value;

            var vendorId = http.User.GetVendorId();
            if (vendorId is not null) entry.VendorId = vendorId.Value;

            var id = await repo.AddEntryAsync(entry, ct);
            return Results.Ok(id);
        });

        group.MapPost("damage-update", async ([FromServices] IDamageService repo, [FromBody] DamageEntry entry, CancellationToken ct) =>
        {
            var id = await repo.UpdateEntryAsync(entry, ct);
            return Results.Ok(id);
        });

        group.MapGet("vendor-get", async (HttpContext http, [FromServices] IVendorService repo, [FromQuery] string placeId, CancellationToken ct) =>
        {
            var vendor = await repo.GetVendorAsync(placeId, null, ct);
            return vendor is null ? Results.NotFound() : Results.Ok(vendor);
        });

        return group;
    }

    //internal static IEndpointConventionBuilder MapPayment(this RouteGroupBuilder group)
    //{
    //    group.MapPost("create-checkout-session", async (StripeClient client) =>
    //    {
    //        var domain = "http://localhost:4242";
    //        var options = new SessionCreateOptions
    //        {
    //            LineItems = [
    //                new SessionLineItemOptions{
    //                    Price = "{{PRICE_ID}}",
    //                    Quantity = 1,
    //            }],
    //            Mode = "payment",
    //            SuccessUrl = $"{domain}/success.html",
    //        };

    //        var service = new SessionService(client);
    //        Session session = await service.CreateAsync(options);
    //        return Results.Redirect(session.Url, permanent: false);
    //    });
    //    return group;
    //}

    internal static IEndpointConventionBuilder MapVendorEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("vendor-profile", async (HttpContext http, [FromServices] IVendorService repo, CancellationToken ct) =>
        {
            var vendorId = http.User.GetVendorId();
            var vendor = await repo.GetVendorAsync(null, vendorId, ct);
            return vendor is null ? Results.NotFound() : Results.Ok(vendor);
        }).RequireAuthorization("Vendor");

        group.MapGet("user-list", async ([FromServices] IUserService repo, CancellationToken ct) =>
        {
            var entries = await repo.GetUsersAsync(ct);
            if (entries == null) return Results.NotFound();
            return Results.Ok(entries);
        }).RequireAuthorization("Admin");

        group.MapGet("roles-get", async ([FromServices] IUserService repo, CancellationToken ct) =>
        {
            var entries = await repo.GetRolesAsync(ct);
            if (entries == null) return Results.NotFound();
            return Results.Ok(entries);
        }).RequireAuthorization("Admin");

        group.MapPost("roles-update", async ([FromServices] IUserService repo, [FromBody] AppUser user, CancellationToken ct) =>
        {
            await repo.UpdateRolesAsync(user, ct);
            return Results.Ok();
        }).RequireAuthorization("Admin");

        return group;
    }
}