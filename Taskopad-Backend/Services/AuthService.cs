using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Taskopad_Backend.Data;
using Taskopad_Backend.DTOs;
using Taskopad_Backend.Models;
using Taskopad_Backend.Services.Interfaces;

namespace Taskopad_Backend.Services
{
    public class AuthService :IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        // Fixed role GUIDs — match seeds in AppDbContext
        private static readonly Dictionary<string, Guid> RoleIds = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Junior"] = new Guid("11111111-1111-1111-1111-111111111111"),
            ["Senior"] = new Guid("22222222-2222-2222-2222-222222222222"),
            ["TechLead"] = new Guid("33333333-3333-3333-3333-333333333333"),
            ["PM"] = new Guid("44444444-4444-4444-4444-444444444444"),
            ["Admin"] = new Guid("55555555-5555-5555-5555-555555555555"),
        };
        public AuthService(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }
        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                throw new InvalidOperationException("Email already registered.");

            if (!RoleIds.TryGetValue(dto.Role, out var roleId))
                throw new ArgumentException($"Unknown role '{dto.Role}'.");

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            };

            _db.Users.Add(user);
            _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
            await _db.SaveChangesAsync();

            return BuildResponse(user, dto.Role);
        }

        // ── Login 
        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == dto.Email)
                ?? throw new UnauthorizedAccessException("Invalid credentials.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials.");

            var roleName = user.UserRoles.First().Role.Name;
            return BuildResponse(user, roleName);
        }

        // ── JWT factory 
        private AuthResponseDto BuildResponse(User user, string role)
        {
            var token = GenerateJwt(user, role);
            return new AuthResponseDto
            {
                Token = token,
                FullName = user.FullName,
                Email = user.Email,
                Role = role,
                UserId = user.Id,
            };
        }
        private string GenerateJwt(User user, string role)
        {
            var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing.");
            var issuer = _config["Jwt:Issuer"] ?? "JiraClone";
            var audience = _config["Jwt:Audience"] ?? "JiraCloneClient";

            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, role),          // used by [Authorize(Roles="...")] and Angular
        };

            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
