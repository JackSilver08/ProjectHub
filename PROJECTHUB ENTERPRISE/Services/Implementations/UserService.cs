using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;
using PROJECTHUB_ENTERPRISE.Models;
using PROJECTHUB_ENTERPRISE.Services.Interfaces;

namespace PROJECTHUB_ENTERPRISE.Services.Implementations;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    public UserService(AppDbContext db) { _db = db; }

    public async Task<User?> GetByIdAsync(Guid userId) =>
        await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

    public async Task<User?> GetByEmailAsync(string email) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

    public async Task<List<UserSearchResult>> SearchAsync(string query, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(query)) return new();
        return await _db.Users.AsNoTracking()
            .Where(u => u.Email.Contains(query) || (u.FullName != null && u.FullName.Contains(query)))
            .Select(u => new UserSearchResult
            {
                Id = u.Id, FullName = u.FullName,
                Email = u.Email, AvatarUrl = u.AvatarUrl
            }).Take(limit).ToListAsync();
    }

    public async Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        if (user == null) return false;
        if (request.FullName != null) user.FullName = request.FullName;
        if (request.Username != null) user.Username = request.Username;
        if (request.Email != null) user.Email = request.Email;
        if (!string.IsNullOrWhiteSpace(request.AvatarUrl)) user.AvatarUrl = request.AvatarUrl;
        if (request.JobTitle != null) user.JobTitle = request.JobTitle;
        if (request.Department != null) user.Department = request.Department;
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<bool> VerifyPasswordAsync(User user, string password)
    {
        return Task.FromResult(BCrypt.Net.BCrypt.Verify(password, user.PasswordHash));
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        if (user == null) return false;
        if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash)) return false;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<string> ResetPasswordAsync(Guid userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) throw new InvalidOperationException("User not found");
        var newPassword = "PH@" + Guid.NewGuid().ToString("N")[..6];
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync();
        return newPassword;
    }
}
