using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public sealed partial class AppDbContext
    : IdentityDbContext<AppUser, AppRole, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<DamageEntry> DamageEntries => Set<DamageEntry>();
    public DbSet<DamageEntrySection> DamageEntrySections => Set<DamageEntrySection>();
    public DbSet<DamageSectionType> DamageSectionTypes => Set<DamageSectionType>();
    public DbSet<VendorModel> Vendors => Set<VendorModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        const string schema = "public";

        modelBuilder
            .ConfigureIdentityDomain(schema)
            .ConfigureDamageDomain(schema)
            .ConfigureVendorDomain(schema);
    }
}