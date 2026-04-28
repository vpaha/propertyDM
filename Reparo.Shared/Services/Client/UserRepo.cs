using System.Net.Http.Json;

public interface IUserRepo
{
    Task<IReadOnlyList<AppUser>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppRole>> GetRolesAsync(CancellationToken ct = default);
    Task UpdateRolesAsync(AppUser user, CancellationToken ct = default);
}

public sealed class UserRepo : IUserRepo
{
    private readonly HttpClient _http;

    public UserRepo(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<AppUser>> GetUsersAsync(CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<IReadOnlyList<AppUser>>($"vendor/user-list", ct) ?? Array.Empty<AppUser>();
    }

    public async Task<IReadOnlyList<AppRole>> GetRolesAsync(CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<IReadOnlyList<AppRole>>($"vendor/roles-get", ct) ?? Array.Empty<AppRole>();
    }

    public async Task UpdateRolesAsync(AppUser user, CancellationToken ct = default)
    {
        await _http.PostAsJsonAsync("vendor/roles-update", user, ct);
    }
}