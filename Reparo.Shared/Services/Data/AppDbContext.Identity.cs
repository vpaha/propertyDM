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
            e.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            e.Property(x => x.UserName).HasColumnName("UserName");
            e.Property(x => x.NormalizedUserName).HasColumnName("NormalizedUserName");
            e.Property(x => x.Email).HasColumnName("Email");
            e.Property(x => x.NormalizedEmail).HasColumnName("NormalizedEmail");
            e.Property(x => x.PhoneNumber).HasColumnName("PhoneNumber");
            e.Property(x => x.LockoutEnd).HasColumnName("LockoutEnd").HasColumnType("timestamp with time zone");
            e.Property(x => x.VendorId).HasColumnName("vendor_id").IsRequired(false);

            e.HasOne<VendorModel>()
                .WithMany()
                .HasForeignKey(x => x.VendorId)
                .HasConstraintName("fk_aspnet_users_vendor_id")
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            e.HasIndex(x => x.NormalizedUserName).IsUnique().HasDatabaseName("UserNameIndex");
            e.HasIndex(x => x.NormalizedEmail).HasDatabaseName("EmailIndex");
            e.HasIndex(x => x.VendorId).HasDatabaseName("ix_aspnet_users_vendor_id");
        });

        modelBuilder.Entity<AppRole>(e =>
        {
            e.ToTable("aspnet_roles", schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            e.Property(x => x.Name).HasColumnName("Name");

            e.Property(x => x.NormalizedName).HasColumnName("NormalizedName");
            e.Property(x => x.ConcurrencyStamp).HasColumnName("ConcurrencyStamp");

            e.HasIndex(x => x.NormalizedName).IsUnique().HasDatabaseName("RoleNameIndex");
        });

        modelBuilder.Entity<IdentityUserClaim<int>>(e =>
        {
            e.ToTable("aspnet_user_claims", schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            e.Property(x => x.UserId).HasColumnName("UserId").IsRequired();
            e.Property(x => x.ClaimType).HasColumnName("ClaimType");
            e.Property(x => x.ClaimValue).HasColumnName("ClaimValue");
            e.HasIndex(x => x.UserId).HasDatabaseName("IX_AspNetUserClaims_UserId");

            e.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_AspNetUserClaims_AspNetUsers_UserId");
        });

        modelBuilder.Entity<IdentityRoleClaim<int>>(e =>
        {
            e.ToTable("aspnet_role_claims", schema);
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            e.Property(x => x.RoleId).HasColumnName("RoleId").IsRequired();
            e.Property(x => x.ClaimType).HasColumnName("ClaimType");
            e.Property(x => x.ClaimValue).HasColumnName("ClaimValue");

            e.HasIndex(x => x.RoleId).HasDatabaseName("IX_AspNetRoleClaims_RoleId");

            e.HasOne<AppRole>().WithMany().HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_AspNetRoleClaims_AspNetRoles_RoleId");
        });

        modelBuilder.Entity<IdentityUserLogin<int>>(e =>
        {
            e.ToTable("aspnet_user_logins", schema);
            e.HasKey(x => new { x.LoginProvider, x.ProviderKey });
            e.Property(x => x.LoginProvider).HasColumnName("LoginProvider");
            e.Property(x => x.ProviderKey).HasColumnName("ProviderKey");
            e.Property(x => x.ProviderDisplayName).HasColumnName("ProviderDisplayName");
            e.Property(x => x.UserId).HasColumnName("UserId");

            e.HasIndex(x => x.UserId).HasDatabaseName("IX_AspNetUserLogins_UserId");
            e.HasOne<AppUser>().WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_AspNetUserLogins_AspNetUsers_UserId");
        });

        modelBuilder.Entity<IdentityUserToken<int>>(e =>
        {
            e.ToTable("aspnet_user_tokens", schema);
            e.HasKey(x => new { x.UserId, x.LoginProvider, x.Name });
            e.Property(x => x.UserId).HasColumnName("UserId");
            e.Property(x => x.LoginProvider).HasColumnName("LoginProvider");
            e.Property(x => x.Name).HasColumnName("Name");
            e.Property(x => x.Value).HasColumnName("Value");

            e.HasOne<AppUser>().WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_AspNetUserTokens_AspNetUsers_UserId");
        });

        modelBuilder.Entity<IdentityUserRole<int>>(e =>
        {
            e.ToTable("aspnet_user_roles", schema);
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.Property(x => x.UserId).HasColumnName("UserId");
            e.Property(x => x.RoleId).HasColumnName("RoleId");
            e.HasIndex(x => x.RoleId).HasDatabaseName("IX_AspNetUserRoles_RoleId");

            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_AspNetUserRoles_AspNetUsers_UserId");

            e.HasOne<AppRole>().WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_AspNetUserRoles_AspNetRoles_RoleId");
        });

        return modelBuilder;
    }
}