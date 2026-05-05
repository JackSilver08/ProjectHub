using PROJECTHUB_ENTERPRISE.Models;

namespace PROJECTHUB_ENTERPRISE.Services.Interfaces;

/// <summary>
/// Service xử lý logic người dùng.
/// </summary>
public interface IUserService
{
    Task<User?> GetByIdAsync(Guid userId);
    Task<User?> GetByEmailAsync(string email);
    Task<List<UserSearchResult>> SearchAsync(string query, int limit = 5);
    Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<bool> VerifyPasswordAsync(User user, string password);
    Task<bool> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);
    Task<string> ResetPasswordAsync(Guid userId);
}

public class UserSearchResult
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string Email { get; set; } = "";
    public string? AvatarUrl { get; set; }
}

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
}
