using ADScimApi.Models;

namespace ADScimApi.Auth;

public interface IAuthService
{
    Task<(bool Success, string Token)> AuthenticateAsync(string username, string password);
    Task<bool> ValidateTokenAsync(string token);
    Task<ApiUser?> GetUserByUsernameAsync(string username);
    Task<IEnumerable<ApiUser>> GetAllUsersAsync();
    Task<ApiUser> CreateUserAsync(ApiUser user, string password);
    Task<ApiUser?> UpdateUserAsync(int id, ApiUser user, string? password = null);
    Task<bool> DeleteUserAsync(int id);
}