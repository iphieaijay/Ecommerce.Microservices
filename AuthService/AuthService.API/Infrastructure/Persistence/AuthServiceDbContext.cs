namespace AuthService.API.Infrastructure.Persistence;

using AuthService.API.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AuthServiceDbContext : IdentityDbContext<AppUser, IdentityRole, string>
{
    public AuthServiceDbContext(DbContextOptions<AuthServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Remove 'AspNet' prefix from Identity tables
        builder.Entity<AppUser>(b => b.ToTable("Users"));
        builder.Entity<IdentityRole>(b => b.ToTable("Roles"));
        builder.Entity<IdentityUserRole<string>>(b => b.ToTable("UserRoles"));
        builder.Entity<IdentityUserClaim<string>>(b => b.ToTable("UserClaims"));
        builder.Entity<IdentityUserLogin<string>>(b => b.ToTable("UserLogins"));
        builder.Entity<IdentityRoleClaim<string>>(b => b.ToTable("RoleClaims"));
        builder.Entity<IdentityUserToken<string>>(b => b.ToTable("UserTokens"));
    }
}

