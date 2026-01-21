using AuthService.API.Domain;
using AuthService.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Shared.Contracts.Responses;
using System;

namespace AuthService.API.Features.LogoutUser
{
    using MediatR;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using System.Threading;
    using System.Threading.Tasks;

    using MediatR;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    
    public sealed class LogoutUserHandler : IRequestHandler<LogoutUserCommand, string>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AuthServiceDbContext _db;
        private readonly IHttpContextAccessor _httpContext;

        public LogoutUserHandler(
            UserManager<AppUser> userManager,
            AuthServiceDbContext db,
            IHttpContextAccessor httpContext)
        {
            _userManager = userManager;
            _db = db;
            _httpContext = httpContext;
        }

        public async Task<string> Handle(LogoutUserCommand request, CancellationToken ct)
        {
            var refreshToken = await _db.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == request.RefreshToken && !x.IsRevoked, ct);

            if (refreshToken is null)
                return "Invalid refresh token";

            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            return "User logged out successfully";
        }
    }


}
