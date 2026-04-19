using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;

public interface IUserRepo
{
    Task<IReadOnlyList<AppUser>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task AddRoleToUserAsync(int userId, string roleName, CancellationToken ct = default);
    Task RemoveRoleFromUserAsync(int userId, int roleId, CancellationToken ct = default);
    Task<IReadOnlyList<AppRole>> GetRolesAsync(CancellationToken ct = default);
}

public sealed class UserRepo : IUserRepo
{
    private readonly HttpClient _http;

    public UserRepo(HttpClient http, AuthenticationStateProvider authProvider)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<AppUser>> GetUsersAsync(CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<IReadOnlyList<AppUser>>($"damage/user-list-get", ct) ?? Array.Empty<AppUser>();
    }

    public async Task AddRoleToUserAsync(int userId, string roleName, CancellationToken ct = default)
    {
        var url = $"damage/users/{userId}/roles/{roleName}";
        var response = await _http.PostAsync(url, null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveRoleFromUserAsync(int userId, int roleId, CancellationToken ct = default)
    {
        var url = $"damage/users/{userId}/roles/{roleId}";
        var response = await _http.DeleteAsync(url, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<AppRole>> GetRolesAsync(CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<IReadOnlyList<AppRole>>($"damage/roles-get", ct) ?? Array.Empty<AppRole>();
    }
}