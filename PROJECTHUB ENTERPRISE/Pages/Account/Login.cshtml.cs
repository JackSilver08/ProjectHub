using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;
using PROJECTHUB_ENTERPRISE.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BCrypt.Net;

namespace PROJECTHUB_ENTERPRISE.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _db;

        public LoginModel(AppDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == Input.Email && u.IsActive);

            if (user == null)
            {
                ErrorMessage = "Invalid email or password";
                return Page();
            }

            // ✅ VERIFY BCRYPT
            if (!BCrypt.Net.BCrypt.Verify(Input.Password, user.PasswordHash))
            {
                ErrorMessage = "Invalid email or password";
                return Page();
            }

            // ✅ Tạo Claims
            var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),

    // Username dùng để hiển thị ở header
    new Claim(ClaimTypes.Name, user.Username),

    // Full name (optional)
    new Claim("FullName",
        string.IsNullOrWhiteSpace(user.FullName)
            ? ""
            : user.FullName),

    new Claim(ClaimTypes.Email, user.Email),

    // ⭐ QUAN TRỌNG: AVATAR
    new Claim("AvatarUrl",
        string.IsNullOrEmpty(user.AvatarUrl)
            ? "/images/default-avatar.png"
            : user.AvatarUrl)
};


            var identity = new ClaimsIdentity(claims, "ProjectHubCookie");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("ProjectHubCookie", principal);

            return RedirectToPage("/Index");

        }

        public class InputModel
        {
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required, DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            public bool RememberMe { get; set; }
        }
    }
}
