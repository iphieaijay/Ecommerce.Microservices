namespace AuthService.Domain
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public required string Token { get; set; }
        public DateTime Expires { get; set; }
        public bool IsRevoked { get; set; } = false;
        public required string UserId { get; set; }
        public DateTime? RevokedAt { get; set; }
        public required AppUser User { get; set; }
    }

}
