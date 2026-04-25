using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

internal static class IdentityModelBuilderExtensions
{
    internal static ModelBuilder ConfigureIdentityDomain(this ModelBuilder modelBuilder, string schema)
    {
        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("aspnet_users", schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.UserName).HasColumnName("UserName");
            e.Property(x => x.Email).HasColumnName("Email");
            e.Property(x => x.PhoneNumber).HasColumnName("PhoneNumber");
            e.Property(x => x.LockoutEnd).HasColumnType("timestamp with time zone");

            e.Property(x => x.VendorId)
                .HasColumnName("Vendor_id").IsRequired(false);

            e.HasOne<VendorModel>()
                .WithMany()
                .HasForeignKey(x => x.VendorId)
                .HasConstraintName("fk_aspnet_users_vendors_vendor_id")
                .OnDelete(DeleteBehavior.SetNull).IsRequired(false);
        });

        modelBuilder.Entity<AppRole>(e =>
        {
            e.ToTable("aspnet_roles", schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<IdentityUserClaim<int>>(e =>
        {
            e.ToTable("aspnet_user_claims", schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<IdentityRoleClaim<int>>(e =>
        {
            e.ToTable("aspnet_role_claims", schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<IdentityUserLogin<int>>(e =>
        {
            e.ToTable("aspnet_user_logins", schema);
            e.HasKey(x => new { x.LoginProvider, x.ProviderKey });
        });

        modelBuilder.Entity<IdentityUserToken<int>>(e =>
        {
            e.ToTable("aspnet_user_tokens", schema);
            e.HasKey(x => new { x.UserId, x.LoginProvider, x.Name });
        });

        modelBuilder.Entity<IdentityUserRole<int>>(e =>
        {
            e.ToTable("aspnet_user_roles", schema);
            e.HasKey(x => new { x.UserId, x.RoleId });
        });

        return modelBuilder;
    }
}