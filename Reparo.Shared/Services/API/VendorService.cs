using Microsoft.EntityFrameworkCore;

public interface IVendorService
{
    Task<VendorModel?> GetVendorByPlaceAsync(string placeId, CancellationToken cancellationToken = default);
    Task<VendorModel?> GetVendorAsync(int vendorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VendorModel>> GetVendorListAsync(CancellationToken cancellationToken = default);

    // not used
    //Task<long> AddVendorAsync(VendorModel vendor, CancellationToken cancellationToken = default);
    //Task UpdateVendorAsync(VendorModel vendor, CancellationToken cancellationToken = default);
    //Task<bool> VendorExistsAsync(string placeId, CancellationToken cancellationToken = default);
}

public sealed class VendorService : IVendorService
{
    private readonly AppDbContext _context;

    public VendorService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<VendorModel?> GetVendorByPlaceAsync(string placeId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<VendorModel>().AsNoTracking().Where(e => e.PlaceId == placeId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<VendorModel?> GetVendorAsync(int vendorId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<VendorModel>().AsNoTracking().Where(e => e.Id == vendorId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VendorModel>> GetVendorListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<VendorModel>().AsNoTracking().Where(v => v.IsActive).OrderBy(v => v.UpdatedAt).ToListAsync(cancellationToken);
    }

    //public async Task<long> AddVendorAsync(VendorModel vendor, CancellationToken cancellationToken = default)
    //{
    //    ArgumentNullException.ThrowIfNull(vendor);

    //    vendor.CreatedAt = DateTimeOffset.UtcNow;
    //    vendor.UpdatedAt = DateTimeOffset.UtcNow;
    //    _context.Set<VendorModel>().Add(vendor);
    //    await _context.SaveChangesAsync(cancellationToken);
    //    return vendor.Id;
    //}

    //public async Task UpdateVendorAsync(VendorModel vendor, CancellationToken cancellationToken = default)
    //{
    //    ArgumentNullException.ThrowIfNull(vendor);
    //    vendor.UpdatedAt = DateTimeOffset.UtcNow;
    //    _context.Set<VendorModel>().Update(vendor);
    //    await _context.SaveChangesAsync(cancellationToken);
    //}

    //public async Task<bool> VendorExistsAsync(string placeId, CancellationToken cancellationToken = default)
    //{
    //    if (string.IsNullOrWhiteSpace(placeId)) return false;
    //    return await _context.Set<VendorModel>().AsNoTracking().AnyAsync(v => v.PlaceId == placeId, cancellationToken);
    //}
}