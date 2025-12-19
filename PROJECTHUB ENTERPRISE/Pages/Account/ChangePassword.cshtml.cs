using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;
using System.Security.Claims;

namespace PROJECTHUB_ENTERPRISE.Pages.Account
{
    public class ChangePasswordModel : PageModel
    {
        private readonly AppDbContext _db;

        public ChangePasswordModel(AppDbContext db)
        {
            _db = db;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync(
            [FromBody] ChangePasswordRequest req)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            var user = await _db.Users.FirstAsync(u => u.Id == userId);

            // ✅ Verify old password
            if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            {
                return BadRequest("Current password is incorrect");
            }

            if (req.NewPassword != req.ConfirmPassword)
            {
                return BadRequest("Password confirmation does not match");
            }

            user.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(req.NewPassword);

            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
    }
}
