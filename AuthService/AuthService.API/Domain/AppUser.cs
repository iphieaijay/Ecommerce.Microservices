using Microsoft.AspNetCore.Identity;

namespace AuthService.API.Domain
{
    public class AppUser: IdentityUser
    {
        public required override string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public override string PhoneNumber { get; set; }
        public string Address { get; set; }
    }
}
