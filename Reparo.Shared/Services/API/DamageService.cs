using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

public interface IDamageService
{
    void InvalidateCache();

    Task<IReadOnlyList<DamageSectionType>> ListSectionTypesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DamageEntry>> ListDamageEntriesAsync(int? userId, CancellationToken ct = default);
    Task<long> AddEntryAsync(DamageEntry entry, CancellationToken ct = default);
    Task<long> UpdateEntryAsync(DamageEntry entry, CancellationToken ct = default);
}

public sealed class DamageService : IDamageService
{
    private const string CacheKey = "damage_section_types:v1";

    private readonly MemoryCacheEntryOptions cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12),
        Priority = CacheItemPriority.High
    };

    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;

    public DamageService(AppDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public void InvalidateCache() => _cache.Remove(CacheKey);

    public async Task<IReadOnlyList<DamageEntry>> ListDamageEntriesAsync(int? userId, CancellationToken ct = default)
    {
        var list = await _context.DamageEntries
                .AsNoTracking()
                .Where(e => !userId.HasValue || e.UserId == userId.Value)
                .Include(e => e.Sections)
                    .ThenInclude(s => s.DamageSectionType)
                .AsSplitQuery()
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync(ct);

        return list;
    }

    public async Task<IReadOnlyList<DamageSectionType>> ListSectionTypesAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyList<DamageSectionType>? cached) && cached is not null)
            return cached;

        var list = await _context.DamageSectionTypes
            .AsNoTracking()
            .OrderBy(d => d.Id)
            .Select(d => new DamageSectionType
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                IsEmergency = d.IsEmergency
            })
            .ToListAsync(ct);

        _cache.Set(CacheKey, list, cacheOptions);
        return list;
    }

    // Adds a DamageEntry only (no sections)
    public async Task<long> AddEntryAsync(DamageEntry entry, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        entry.CreatedAt = entry.CreatedAt == default ? now : entry.CreatedAt;
        entry.UpdatedAt = entry.UpdatedAt == default ? now : entry.UpdatedAt;

        if (entry.Sections is { Count: > 0 })
        {
            foreach (var s in entry.Sections)
            {
                s.DamageSectionType = null;
                s.CreatedAt = s.CreatedAt == default ? now : s.CreatedAt;
            }
        }

        _context.DamageEntries.Add(entry);
        await _context.SaveChangesAsync(ct);

        return entry.Id;
    }

    public async Task<long> UpdateEntryAsync(DamageEntry entry, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        entry.UpdatedAt = entry.UpdatedAt == default ? now : entry.UpdatedAt;
        if (entry.DateOfLoss.Offset != TimeSpan.Zero) entry.DateOfLoss = now;

        _context.DamageEntries.Update(entry);

        await _context.SaveChangesAsync(ct);
        return entry.Id;
    }
}