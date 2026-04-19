using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public interface IUserService
{
    Task ProvisionAsync(ClaimsPrincipal principal, CancellationToken ct = default);
    Task<IReadOnlyList<AppUser>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task AddRoleToUserAsync(int userId, string roleName, CancellationToken ct = default);
    Task RemoveRoleFromUserAsync(int userId, int roleId, CancellationToken ct = default);
    Task<IReadOnlyList<AppRole>> GetRolesAsync(CancellationToken ct = default);
}

public sealed class UserService : BaseService, IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context) : base()
    {
        _context = context;
    }

    public async Task ProvisionAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        if (principal?.Identity?.IsAuthenticated != true) return;

        var providerKey =
            principal.FindFirst("sub")?.Value ??
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            throw new InvalidOperationException("Missing provider user id claim.");

        var loginProvider =
            principal.Identity?.AuthenticationType ??
            principal.FindFirst("iss")?.Value ??
            "external";

        var userId = await _context.UserLogins
            .AsNoTracking()
            .Where(x => x.LoginProvider == loginProvider && x.ProviderKey == providerKey)
            .Select(x => (int?)x.UserId)
            .SingleOrDefaultAsync(ct);

        if (!userId.HasValue)
        {
            userId = await CreateUserWithLoginAsync(principal, loginProvider, providerKey, ct);
        }

        var roleNames = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId.Value)
            .Join(
                _context.Roles.AsNoTracking(),
                ur => ur.RoleId,
                r => r.Id,
                (_, r) => r.Name!)
            .ToListAsync(ct);

        if (principal.Identity is ClaimsIdentity identity)
        {
            foreach (var claim in identity.FindAll(identity.RoleClaimType).ToList()) identity.RemoveClaim(claim);
            foreach (var role in roleNames) identity.AddClaim(new Claim(identity.RoleClaimType, role));
        }
    }

    public async Task<IReadOnlyList<AppUser>> GetUsersAsync(CancellationToken ct = default)
    {
        var users = await _context.Users.AsNoTracking().ToListAsync(ct);
        var userIds = users.Select(u => u.Id).ToArray();

        var logins = await _context.UserLogins.AsNoTracking().Where(x => userIds.Contains(x.UserId)).ToListAsync(ct);
        var roleMap = await _context.UserRoles.AsNoTracking().Where(ur => userIds.Contains(ur.UserId))
            .Join(
                _context.Roles.AsNoTracking(),
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, Role = new AppRole { Id = r.Id, Name = r.Name!, NormalizedName = r.NormalizedName } })
            .ToListAsync(ct);

        foreach (var user in users)
        {
            user.ProviderDisplayName = logins.FirstOrDefault(x => x.UserId == user.Id)?.ProviderDisplayName;
            user.Roles = roleMap.Where(x => x.UserId == user.Id).Select(x => x.Role).ToArray();
        }
        return users;
    }

    private async Task<int> CreateUserWithLoginAsync(ClaimsPrincipal principal, string loginProvider, string providerKey, CancellationToken ct)
    {
        var userName =
            principal.FindFirst("preferred_username")?.Value ??
            principal.FindFirst(ClaimTypes.Email)?.Value ??
            principal.FindFirst("email")?.Value ??
            principal.FindFirst(ClaimTypes.Name)?.Value ??
            $"user_{Guid.NewGuid():N}";

        var email =
            principal.FindFirst(ClaimTypes.Email)?.Value ??
            principal.FindFirst("email")?.Value;

        var phoneNumber =
            principal.FindFirst(ClaimTypes.MobilePhone)?.Value ??
            principal.FindFirst(ClaimTypes.HomePhone)?.Value ??
            principal.FindFirst("phone_number")?.Value;

        var user = new AppUser
        {
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email?.ToUpperInvariant(),
            PhoneNumber = phoneNumber,
            EmailConfirmed = !string.IsNullOrWhiteSpace(email)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        var login = new IdentityUserLogin<int>
        {
            UserId = user.Id,
            LoginProvider = loginProvider,
            ProviderKey = providerKey,
            ProviderDisplayName = loginProvider
        };

        _context.UserLogins.Add(login);
        await _context.SaveChangesAsync(ct);

        return user.Id;
    }

    public async Task AddRoleToUserAsync(int userId, string roleName, CancellationToken ct = default)
    {
        var role = await _context.Roles.SingleOrDefaultAsync(r => r.Name == roleName, ct) ?? throw new InvalidOperationException($"Role '{roleName}' was not found.");
        var exists = await _context.UserRoles.AnyAsync(x => x.UserId == userId && x.RoleId == role.Id, ct);
        if (exists) return;

        _context.UserRoles.Add(new IdentityUserRole<int>
        {
            UserId = userId,
            RoleId = role.Id
        });

        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoveRoleFromUserAsync(int userId, int roleId, CancellationToken ct = default)
    {
        var userRole = await _context.UserRoles.SingleOrDefaultAsync(x => x.UserId == userId && x.RoleId == roleId, ct);
        if (userRole is null) return;

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AppRole>> GetRolesAsync(CancellationToken ct = default)
    {
        return await _context.Roles
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new AppRole
            {
                Id = x.Id,
                Name = x.Name,
                NormalizedName = x.NormalizedName,
                ConcurrencyStamp = x.ConcurrencyStamp
            })
            .ToListAsync(ct);
    }
}