using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;
using PROJECTHUB_ENTERPRISE.Models;
using System.Security.Claims;

namespace PROJECTHUB_ENTERPRISE.Pages.Account
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly AppDbContext _context;

        public ProfileModel(AppDbContext context)
        {
            _context = context;
        }

        public User CurrentUser { get; private set; } = null!;

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdStr, out var userId))
            {
                return RedirectToPage("/Account/Login");
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            CurrentUser = user;
            return Page();
        }

        // ================= EDIT PROFILE =================
        [BindProperty]
        public EditProfileDto EditProfile { get; set; } = new();
        public async Task<IActionResult> OnPostEditProfileAsync()
        {
            var dto = EditProfile;

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
                return new JsonResult(new { success = false });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null)
                return new JsonResult(new { success = false });

            user.FullName = dto.FullName ?? user.FullName;
            user.Username = dto.Username ?? user.Username;
            user.Email = dto.Email ?? user.Email;

            if (!string.IsNullOrWhiteSpace(dto.AvatarUrl))
                user.AvatarUrl = dto.AvatarUrl;


            if (!string.IsNullOrWhiteSpace(dto.AvatarUrl))
                user.AvatarUrl = dto.AvatarUrl;

            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                avatarUrl = user.AvatarUrl,
                fullName = user.FullName,
                username = user.Username,
                email = user.Email
            });
        }

    }
}
