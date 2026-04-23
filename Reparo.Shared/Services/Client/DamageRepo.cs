using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;

public interface IDamageRepo
{
    Task<IReadOnlyList<DamageSectionType>> ListSectionTypesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DamageEntry>> ListDamageEntriesAsync(CancellationToken ct = default);
    Task<long> AddEntryAsync(DamageEntry entry, CancellationToken ct = default);
    Task<long> UpdateEntryAsync(DamageEntry entry, CancellationToken ct = default);
}

public sealed class DamageRepo : IDamageRepo
{
    private readonly HttpClient _http;
    private readonly AuthenticationStateProvider _authProvider;

    public DamageRepo(HttpClient http, AuthenticationStateProvider authProvider)
    {
        _http = http;
        _authProvider = authProvider;
    }

    private async Task<int> GetUserAsync()
    {
        var authState = await _authProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        var userId = 1;
        if (user.Identity?.IsAuthenticated == true)
        {
            var appUserId = user.FindFirst("app_user_id")?.Value;
            if (!string.IsNullOrWhiteSpace(appUserId) && int.TryParse(appUserId, out var parsedUserId)) userId = parsedUserId;
        }

        return userId;
    }
    public async Task<IReadOnlyList<DamageSectionType>> ListSectionTypesAsync(CancellationToken ct = default)
    {
        var list = await _http.GetFromJsonAsync<IReadOnlyList<DamageSectionType>>("damage/damage-sections", ct);
        return list ?? Array.Empty<DamageSectionType>();
    }

    public async Task<IReadOnlyList<DamageEntry>> ListDamageEntriesAsync(CancellationToken ct = default)
    {
        var userId = await GetUserAsync();
        var list = await _http.GetFromJsonAsync<IReadOnlyList<DamageEntry>>($"damage/damage-entries?userId={userId}", ct);
        return list ?? Array.Empty<DamageEntry>();
    }

    public async Task<long> AddEntryAsync(DamageEntry entry, CancellationToken ct = default)
    {
        entry.UserId = await GetUserAsync();

        var response = await _http.PostAsJsonAsync("damage/damage-add", entry, ct);
        response.EnsureSuccessStatusCode();

        var id = await response.Content.ReadFromJsonAsync<long>(cancellationToken: ct);
        return id;
    }

    public async Task<long> UpdateEntryAsync(DamageEntry entry, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("damage/damage-update", entry, ct);
        response.EnsureSuccessStatusCode();

        var id = await response.Content.ReadFromJsonAsync<long>(cancellationToken: ct);
        return id;
    }
}