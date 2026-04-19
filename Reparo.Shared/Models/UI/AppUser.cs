using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

public sealed class AppUser : IdentityUser<int>
{
    [NotMapped]
    public string? ProviderDisplayName { get; set; }

    [NotMapped]
    public AppRole[]? Roles { get; set; }

    [NotMapped]
    public string RoleNames => Roles is null || Roles.Length == 0 ? string.Empty : string.Join(", ", Roles.Select(r => r.Name));
}

public sealed class AppRole : IdentityRole<int> { }