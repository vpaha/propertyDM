using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;
using Syncfusion.Licensing;

internal class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        var syncfusionKey = builder.Configuration["Syncfusion:LicenseKey"];
        if (!string.IsNullOrWhiteSpace(syncfusionKey)) SyncfusionLicenseProvider.RegisterLicense(syncfusionKey);

        builder.Services.AddSyncfusionBlazor(options => { options.Animation = GlobalAnimationMode.Enable; options.EnableRippleEffect = true; });

        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddAuthenticationStateDeserialization();

        await builder.Build().RunAsync();
    }
}