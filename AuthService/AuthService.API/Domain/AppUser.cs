using Microsoft.AspNetCore.Identity;

namespace AuthService.Domain
{
    public class AppUser : IdentityUser<Guid>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public override string? PhoneNumber { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string? ProfileImageUrl { get; set; }

        // Navigation properties
        public virtual ICollection<AppUserRole> UserRoles { get; set; } = new List<AppUserRole>();
    }

    public class ApplicationRole : IdentityRole<Guid>
    {
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<AppUserRole> UserRoles { get; set; } = new List<AppUserRole>();
    }

    public class AppUserRole : IdentityUserRole<Guid>
    {
        public virtual AppUser User { get; set; } = null!;
        public virtual ApplicationRole Role { get; set; } = null!;
    }

    public class RevokedToken
    {
        public Guid Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public DateTime RevokedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
