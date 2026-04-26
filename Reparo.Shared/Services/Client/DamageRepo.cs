using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;

public interface IDamageRepo
{
    Task<IReadOnlyList<DamageSectionType>> ListSectionTypesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DamageEntry>> ListDamageUserEntriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DamageEntry>> ListDamageVendorEntriesAsync(CancellationToken ct = default);
    Task<long> AddEntryAsync(DamageEntry entry, CancellationToken ct = default);
    Task<long> UpdateEntryAsync(DamageEntry entry, CancellationToken ct = default);
}

public sealed class DamageRepo : BaseRepo, IDamageRepo
{
    private readonly HttpClient _http;

    public DamageRepo(HttpClient http, AuthenticationStateProvider authProvider) : base(authProvider)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<DamageSectionType>> ListSectionTypesAsync(CancellationToken ct = default)
    {
        var list = await _http.GetFromJsonAsync<IReadOnlyList<DamageSectionType>>("damage/damage-sections", ct);
        return list ?? Array.Empty<DamageSectionType>();
    }

    public async Task<IReadOnlyList<DamageEntry>> ListDamageUserEntriesAsync(CancellationToken ct = default)
    {
        var list = await _http.GetFromJsonAsync<IReadOnlyList<DamageEntry>>("damage/damage-user-entries", ct);
        return list ?? Array.Empty<DamageEntry>();
    }

    public async Task<IReadOnlyList<DamageEntry>> ListDamageVendorEntriesAsync(CancellationToken ct = default)
    {
        var list = await _http.GetFromJsonAsync<IReadOnlyList<DamageEntry>>("damage/damage-vendor-entries", ct);
        return list ?? Array.Empty<DamageEntry>();
    }

    public async Task<long> AddEntryAsync(DamageEntry entry, CancellationToken ct = default)
    {
        var userId = await ResolveUserIdAsync();
        if (userId is not null) entry.UserId = userId.Value;

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