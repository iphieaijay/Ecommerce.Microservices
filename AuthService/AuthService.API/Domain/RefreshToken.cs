namespace AuthService.API.Domain
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public DateTime Expires { get; set; }
        public bool IsRevoked { get; set; } = false;
        public string UserId { get; set; }
        public DateTime? RevokedAt { get; set; }
        public AppUser User { get; set; }
    }

}
