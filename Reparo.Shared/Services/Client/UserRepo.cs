using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;

public interface IUserRepo
{
    Task<IReadOnlyList<AppUser>> GetUsersAsync(CancellationToken cancellationToken = default);
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
}