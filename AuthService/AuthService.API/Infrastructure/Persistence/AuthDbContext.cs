using AuthService.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence;

public class AuthDbContext : IdentityDbContext<
    AppUser,
    ApplicationRole,
    Guid,
    IdentityUserClaim<Guid>,
    AppUserRole,
    IdentityUserLogin<Guid>,
    IdentityRoleClaim<Guid>,
    IdentityUserToken<Guid>>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Rename Identity tables
        builder.Entity<AppUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<AppUserRole>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        // AppUser configuration
        builder.Entity<AppUser>(entity =>
        {
            entity.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.RefreshToken)
                .HasMaxLength(500);

            entity.Property(u => u.ProfileImageUrl)
                .HasMaxLength(500);

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.HasIndex(u => u.UserName)
                .IsUnique();
        });

        // ApplicationRole configuration
        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(r => r.Description)
                .HasMaxLength(500);
        });

        // ApplicationUserRole configuration
        builder.Entity<AppUserRole>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });

            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RevokedToken configuration
        builder.Entity<RevokedToken>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Token)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(t => t.Reason)
                .IsRequired()
                .HasMaxLength(500);

            entity.HasIndex(t => t.Token)
                .IsUnique();

            entity.HasIndex(t => t.UserId);

            entity.HasIndex(t => t.ExpiresAt);
        });
    }
}

public static class SeedData
{
    public static async Task InitializeAsync(
        UserManager<AppUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        // Seed Roles
        string[] roleNames = { "Admin", "User", "Manager", "Support" };

        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole
                {
                    Name = roleName,
                    Description = $"{roleName} role",
                    CreatedAt = DateTime.UtcNow
                };
                await roleManager.CreateAsync(role);
            }
        }

        // Seed Admin User
        var adminEmail = "admin@authservice.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                UserName = "admin",
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123456");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}