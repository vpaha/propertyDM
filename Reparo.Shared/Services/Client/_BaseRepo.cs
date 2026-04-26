using Microsoft.AspNetCore.Components.Authorization;

public class BaseRepo
{
    private readonly AuthenticationStateProvider _authProvider;

    public BaseRepo(AuthenticationStateProvider authProvider)
    {
        _authProvider = authProvider;
    }

    protected async Task<int?> ResolveUserIdAsync()
    {
        var authState = await _authProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            var appUserId = user.FindFirst("app_user_id")?.Value;
            if (!string.IsNullOrWhiteSpace(appUserId) && int.TryParse(appUserId, out var parsedUserId)) return parsedUserId;
        }
        return null;
    }

    protected async Task<int?> ResolveVendorIdAsync()
    {
        var authState = await _authProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            var vendorId = user.FindFirst("vendor_id")?.Value;
            if (!string.IsNullOrWhiteSpace(vendorId) && int.TryParse(vendorId, out var parsedVendorId)) return parsedVendorId;
        }
        return null;
    }
}