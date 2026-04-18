using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenAI;
using Serilog;
using Stripe;
using Syncfusion.Blazor;
using Syncfusion.Blazor.Popups;
using Syncfusion.Blazor.SmartComponents;
using Syncfusion.Licensing;
using System.Globalization;
using System.Net.Http.Headers;

namespace Reparo;

public partial class Program
{
    public static void Main(string[] args) => new Program().Run(args);

    private void Run(string[] args)
    {
        RegisterSyncfusion();

        var builder = WebApplication.CreateBuilder(args);

        AddUserSecretsIfDev(builder);
        ConfigureSerilog(builder);

        builder.WebHost.UseStaticWebAssets();

        var config = builder.Configuration;

        var pathBase = NormalizePathBase(config["AppBasePath"] ?? throw new InvalidOperationException("AppBasePath is not configured"));
        var googleApiKey = config["GoogleMaps:ApiKey"] ?? string.Empty;
        var detailedErrors = config.GetValue("DetailedErrors", false);

        ConfigureFrameworkServices(builder, detailedErrors);
        ConfigureLocalization(builder);
        ConfigureAppServices(builder, config, pathBase);

        var app = builder.Build();

        ConfigureForwardedHeaders(app);
        ConfigureErrorHandling(app);
        ConfigurePipeline(app, pathBase);

        MapEndpoints(app, googleApiKey);
        MapRazorComponents(app);

        TryRun(app);
    }

    private void RegisterSyncfusion()
    {
        SyncfusionLicenseProvider.RegisterLicense(
            "Ngo9BigBOggjHTQxAR8/V1JFaF5cXGRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWH5cd3RQRmhZVUJ/XEtWYEA=");
    }

    private void AddUserSecretsIfDev(WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
            builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);
    }

    private void ConfigureSerilog(WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, services, cfg) =>
        {
            cfg.ReadFrom.Configuration(ctx.Configuration).ReadFrom.Services(services);
            if (ctx.HostingEnvironment.IsDevelopment()) cfg.WriteTo.Console().WriteTo.Debug();
            else
            {
                var logDirectory = Path.Combine(builder.Environment.ContentRootPath, "Logs");
                Directory.CreateDirectory(logDirectory);
                cfg.WriteTo.File(Path.Combine(logDirectory, "log.txt"), rollingInterval: RollingInterval.Day);
            }
        });
        builder.Logging.ClearProviders();
    }

    private void ConfigureFrameworkServices(WebApplicationBuilder builder, bool detailedErrors)
    {
        builder.Services.AddHostAuthentication(builder.Configuration);

        builder.Services.AddRazorComponents(o => o.DetailedErrors = detailedErrors)
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents()
            .AddAuthenticationStateSerialization(o => o.SerializeAllClaims = true);

        builder.Services.AddSyncfusionBlazor(o =>
        {
            o.Animation = GlobalAnimationMode.Enable;
            o.EnableRippleEffect = true;
        });

        builder.Services.AddHttpLogging(o => o.LoggingFields = HttpLoggingFields.All);

        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("SignedInUser", p => p.RequireRole("user", "admin"));
            options.AddPolicy("Vendor", p => p.RequireRole("vendor"));
        });

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddMemoryCache();

        builder.Services.AddOutputCache(options =>
        {
            options.AddPolicy("DamageSections", p => p.Expire(TimeSpan.FromHours(12)).Tag("damage-sections"));
        });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });
    }

    private void ConfigureAppServices(WebApplicationBuilder builder, IConfiguration config, string configuredPathBase)
    {
        builder.Services.AddSingleton(new StripeClient("sk_test_51TG7e18LrDbU9B5cfEkBWsaoGNguzvdvvG8qwuUm51eVx4ctFMAyg3R3GfxjJhUbVrRoaK7W8YfnDsIB4NtCTli500uJWmhLHq"));
        // UI
        builder.Services.AddScoped<ToastService>();
        builder.Services.AddScoped<SfDialogService>();
        builder.Services.AddScoped<DamageState>();
        // Repos
        builder.Services.AddScoped<IDamageService, DamageService>();
        builder.Services.AddScoped<IVendorService, VendorService>();
        builder.Services.AddScoped<IUserService, UserService>();
        // Local API
        builder.Services.AddHttpClient<IDamageRepo, DamageRepo>((sp, client) =>
        {
            var ctx = sp.GetRequiredService<IHttpContextAccessor>().HttpContext ?? throw new InvalidOperationException("HttpContext is not available (request scope required).");
            client.BaseAddress = BuildBaseAddressFromRequest(ctx.Request, configuredPathBase);
        });
        builder.Services.AddHttpClient<IVendorRepo, VendorRepo>((sp, client) =>
        {
            var ctx = sp.GetRequiredService<IHttpContextAccessor>().HttpContext ?? throw new InvalidOperationException("HttpContext is not available (request scope required).");
            client.BaseAddress = BuildBaseAddressFromRequest(ctx.Request, configuredPathBase);
        });
        builder.Services.AddHttpClient<IUserRepo, UserRepo>((sp, client) =>
        {
            var ctx = sp.GetRequiredService<IHttpContextAccessor>().HttpContext ?? throw new InvalidOperationException("HttpContext is not available (request scope required).");
            client.BaseAddress = BuildBaseAddressFromRequest(ctx.Request, configuredPathBase);
        });
        // Outbound token
        builder.Services.AddScoped<TokenHandler>();

        // Corporate API
        var claimUri = config["Endpoints:Api:ClaimUri"] ?? throw new InvalidOperationException("Endpoints:Api:ClaimUri is not configured.");

        builder.Services.AddHttpClient<IClaimService, ClaimService>(client =>
        {
            client.BaseAddress = new Uri(claimUri);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddHttpMessageHandler<TokenHandler>();

        // Optional AI
        ConfigureOpenAI(builder, config);

        // DB
        builder.Services.AddDbContext<AppDbContext>(opt =>
        {
            opt.UseNpgsql(
                config.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null));
        });

        builder.Services.AddIdentityCore<AppUser>().AddRoles<AppRole>().AddEntityFrameworkStores<AppDbContext>();
    }

    private void ConfigureOpenAI(WebApplicationBuilder builder, IConfiguration config)
    {
        var key = config["AI:openAIApiKey"];
        var model = config["AI:openAIModel"];

        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(model))
            return;

        var openAIClient = new OpenAIClient(key);
        IChatClient chatClient = openAIClient.GetChatClient(model).AsIChatClient();

        builder.Services.AddChatClient(chatClient);
        builder.Services.AddSyncfusionSmartComponents().InjectOpenAIInference();
    }

    private void ConfigurePipeline(WebApplication app, string pathBase)
    {
        app.UsePathBase(pathBase);

        app.UseSerilogRequestLogging();
        app.UseStatusCodePagesWithReExecute("/notfound", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseHttpLogging();

        app.UseRouting();

        app.UseRequestLocalization();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapStaticAssets();

        app.UseAntiforgery();
        app.UseOutputCache();

        app.UseCors("AllowAll");
    }

    private void MapEndpoints(WebApplication app, string googleApiKey)
    {
        app.MapGroup("authentication").MapLoginAndLogout();
        app.MapGroup("config").MapConfig().DisableAntiforgery();
        app.MapGroup("googlemap").MapGoogleMapEndpoints(googleApiKey);
        app.MapGroup("damage").MapDataBase();
        app.MapGroup("pay").MapPayment();
    }

    private void MapRazorComponents(WebApplication app)
    {
        app.MapRazorComponents<App>()
           .AddInteractiveServerRenderMode()
           .AddInteractiveWebAssemblyRenderMode()
           .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);
    }

    private void ConfigureForwardedHeaders(WebApplication app)
    {
        var forwarded = new ForwardedHeadersOptions
        {
            ForwardedHeaders =
                ForwardedHeaders.XForwardedProto |
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedHost,
            RequireHeaderSymmetry = false
        };

        forwarded.KnownIPNetworks.Clear();
        forwarded.KnownProxies.Clear();

        app.UseForwardedHeaders(forwarded);
    }

    private void ConfigureErrorHandling(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
            return;
        }

        app.UseExceptionHandler("/error", createScopeForErrors: true);
        app.UseHsts();
    }

    private Uri BuildBaseAddressFromRequest(HttpRequest req, string configuredPathBase)
    {
        var pb = req.PathBase.HasValue ? req.PathBase.Value : configuredPathBase;
        var basePath = string.IsNullOrWhiteSpace(pb) ? "/" : pb.Trim().TrimEnd('/') + "/";

        return new UriBuilder
        {
            Scheme = req.Scheme,
            Host = req.Host.Host,
            Port = req.Host.Port ?? -1,
            Path = basePath
        }.Uri;
    }

    private void TryRun(WebApplication app)
    {
        try
        {
            app.Run();
        } catch (Exception ex)
        {
            Log.Fatal(ex, "The application failed to start correctly");
        } finally
        {
            Log.CloseAndFlush();
        }
    }

    private string NormalizePathBase(string pathBase)
    {
        if (string.IsNullOrWhiteSpace(pathBase)) return string.Empty;
        if (!pathBase.StartsWith('/')) pathBase = "/" + pathBase;
        return pathBase.TrimEnd('/');
    }

    private void ConfigureLocalization(WebApplicationBuilder builder)
    {
        var cultures = new CultureData();

        var cultureList = cultures.CultureList;
        cultureList.First(c => c.Name == "en-US").DateTimeFormat.ShortDatePattern = "MM/dd/yyyy";

        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(cultures.DefaultCulture);
            options.SupportedCultures = cultureList;
            options.SupportedUICultures = cultureList;

            options.RequestCultureProviders =
            [
                new CookieRequestCultureProvider()
            ];
        });
    }
}