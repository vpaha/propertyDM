using Microsoft.EntityFrameworkCore;

internal static class VendorModelBuilderExtensions
{
    internal static ModelBuilder ConfigureVendorDomain(this ModelBuilder modelBuilder, string schema)
    {
        modelBuilder.Entity<VendorModel>(e =>
        {
            e.ToTable("vendors", schema);

            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(x => x.LegalName).HasColumnName("legal_name").HasMaxLength(250);
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.PlaceId).HasColumnName("place_id").HasMaxLength(255);
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(320);
            e.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(50);
            e.Property(x => x.WebsiteUrl).HasColumnName("website_url").HasMaxLength(500);
            e.Property(x => x.AddressLine1).HasColumnName("address_line1").HasMaxLength(200);
            e.Property(x => x.AddressLine2).HasColumnName("address_line2").HasMaxLength(200);
            e.Property(x => x.City).HasColumnName("city").HasMaxLength(100);
            e.Property(x => x.State).HasColumnName("state").HasMaxLength(100);
            e.Property(x => x.PostalCode).HasColumnName("postal_code").HasMaxLength(20);
            e.Property(x => x.Country).HasColumnName("country").HasMaxLength(100).HasDefaultValue("US");
            e.Property(x => x.Latitude).HasColumnName("latitude").HasColumnType("numeric(9,6)");
            e.Property(x => x.Longitude).HasColumnName("longitude").HasColumnType("numeric(9,6)");
            e.Property(x => x.LicenseNumber).HasColumnName("license_number").HasMaxLength(100);
            e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(x => x.IsVerified).HasColumnName("is_verified").HasDefaultValue(false);
            e.Property(x => x.IsPreferred).HasColumnName("is_preferred").HasDefaultValue(false);
            e.Property(x => x.Rating).HasColumnName("rating").HasColumnType("numeric(3,2)");
            e.Property(x => x.ReviewCount).HasColumnName("review_count");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            e.HasIndex(x => x.PlaceId).IsUnique().HasDatabaseName("uq_vendors_place_id");
        });

        return modelBuilder;
    }
}