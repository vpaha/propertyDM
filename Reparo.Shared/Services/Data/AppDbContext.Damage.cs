using Microsoft.EntityFrameworkCore;

internal static class ModelBuilderExtensions
{
    internal static ModelBuilder ConfigureDamageDomain(this ModelBuilder modelBuilder, string schema)
    {
        modelBuilder.Entity<DamageSectionType>(e =>
        {
            e.ToTable("damage_section_types", schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.Description).HasColumnName("description").IsRequired();
            e.Property(x => x.IsEmergency).HasColumnName("is_emergency").HasDefaultValue(false);
            e.HasIndex(x => x.Name).IsUnique().HasDatabaseName("damage_section_types_name_key");
        });

        modelBuilder.Entity<DamageEntry>(e =>
        {
            e.ToTable("damage_entries", schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();

            e.Property(x => x.StatusId).HasColumnName("status_id").HasDefaultValue(1).IsRequired();
            e.Property(x => x.AddressEntry).HasColumnName("address_entry").IsRequired();

            e.Property(x => x.ContactEntry).HasColumnName("contact_entry").IsRequired();
            e.Property(x => x.Street).HasColumnName("street");
            e.Property(x => x.City).HasColumnName("city");
            e.Property(x => x.State).HasColumnName("state");
            e.Property(x => x.Zip).HasColumnName("zip");
            e.Property(x => x.Placename).HasColumnName("placename");
            e.Property(x => x.Region).HasColumnName("region");
            e.Property(x => x.InsuranceEntry).HasColumnName("insurance_entry");
            e.Property(x => x.InsuranceCarrier).HasColumnName("insurance_carrier");
            e.Property(x => x.PolicyNumber).HasColumnName("policy_number");
            e.Property(x => x.ClaimNumber).HasColumnName("claim_number");
            e.Property(x => x.FullName).HasColumnName("full_name");
            e.Property(x => x.Phone).HasColumnName("phone");
            e.Property(x => x.Email).HasColumnName("email");

            e.Property(x => x.DateOfLoss).HasColumnName("date_of_loss").HasColumnType("timestamptz").HasDefaultValueSql("now()");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").HasDefaultValueSql("now()");

            e.Property(x => x.Latitude).HasColumnName("latitude").HasColumnType("double precision");
            e.Property(x => x.Longitude).HasColumnName("longitude").HasColumnType("double precision");

            e.Property(x => x.GoogleId).HasColumnName("google_id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.VendorId).HasColumnName("vendor_id");

            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DamageEntrySection>(e =>
        {
            e.ToTable("damage_entry_sections", schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(x => x.DamageEntryId).HasColumnName("damage_entry_id").IsRequired();
            e.Property(x => x.DamageSectionId).HasColumnName("damage_section_id").IsRequired();

            e.Property(x => x.Entry).HasColumnName("entry");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()");
            e.HasIndex(x => new { x.DamageEntryId, x.DamageSectionId }).IsUnique()
                .HasDatabaseName("uq_damage_entry_sections");

            e.HasIndex(x => x.DamageEntryId).HasDatabaseName("ix_damage_entry_sections_damage_entry_id");
            e.HasIndex(x => x.DamageSectionId).HasDatabaseName("ix_damage_entry_sections_damage_section_id");

            e.HasOne(x => x.DamageSectionType).WithMany().HasForeignKey(x => x.DamageSectionId).OnDelete(DeleteBehavior.NoAction);
        });
        return modelBuilder;
    }
}