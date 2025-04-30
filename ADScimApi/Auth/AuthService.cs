using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ADScimApi.Data;
using ADScimApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;

namespace ADScimApi.Auth;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IConfiguration configuration, ILogger<AuthService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _dbContext = null!; // Will be initialized in production
    }

    public async Task<(bool Success, string Token)> AuthenticateAsync(string username, string password)
    {
        // For development/testing, accept admin/admin123
        if (username == "admin" && password == "admin123")
        {
            var user = new ApiUser
            {
                Id = 1,
                Username = "admin",
                Role = "Admin",
                IsActive = true
            };
            
            // Generate JWT token
            var token = GenerateJwtToken(user);
            
            return (true, token);
        }
        
        return (false, string.Empty);
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:Secret"] ?? "DefaultSecretKeyForDevelopment12345678901234");
            
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);
            
            var jwtToken = (JwtSecurityToken)validatedToken;
            var username = jwtToken.Claims.First(x => x.Type == ClaimTypes.Name).Value;
            
            // For development/testing, just check if username is admin
            return username == "admin";
        }
        catch
        {
            return false;
        }
    }

    public async Task<ApiUser?> GetUserByUsernameAsync(string username)
    {
        // For development/testing, return a mock user
        if (username == "admin")
        {
            return new ApiUser
            {
                Id = 1,
                Username = "admin",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            };
        }
        
        return null;
    }

    public async Task<IEnumerable<ApiUser>> GetAllUsersAsync()
    {
        // For development/testing, return a list with one mock user
        return new List<ApiUser>
        {
            new ApiUser
            {
                Id = 1,
                Username = "admin",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            }
        };
    }

    public async Task<ApiUser> CreateUserAsync(ApiUser user, string password)
    {
        // For development/testing, just return the user with an ID
        user.Id = 2; // Admin is ID 1
        user.CreatedAt = DateTime.UtcNow;
        user.PasswordHash = "hashed_" + password;
        
        return user;
    }

    public async Task<ApiUser?> UpdateUserAsync(int id, ApiUser user, string? password = null)
    {
        // For development/testing, just return the updated user
        if (id == 1 && user.Username == "admin")
        {
            return new ApiUser
            {
                Id = 1,
                Username = "admin",
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow
            };
        }
        
        return null;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        // For development/testing, return true for any ID except 1 (admin)
        return id != 1;
    }

    private string GenerateJwtToken(ApiUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:Secret"] ?? "DefaultSecretKeyForDevelopment12345678901234");
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = DateTime.UtcNow.AddHours(24),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}