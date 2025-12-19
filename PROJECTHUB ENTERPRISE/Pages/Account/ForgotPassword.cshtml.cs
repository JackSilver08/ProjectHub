using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;

namespace PROJECTHUB_ENTERPRISE.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly AppDbContext _db;

        public ForgotPasswordModel(AppDbContext db)
        {
            _db = db;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostResetPasswordAsync([FromBody] string email)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return NotFound();

            var newPassword = "PH@" + Guid.NewGuid().ToString("N")[..6];

            user.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(newPassword);

            await _db.SaveChangesAsync();

            return new JsonResult(new
            {
                password = newPassword
            });
        }
    }
}
