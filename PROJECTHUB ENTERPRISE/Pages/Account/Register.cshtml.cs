using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;
using PROJECTHUB_ENTERPRISE.Models;
using BCrypt.Net;

namespace PROJECTHUB_ENTERPRISE.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly AppDbContext _context;

        public RegisterModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // 🔍 Check email tồn tại
            var exists = await _context.Users
                .AnyAsync(u => u.Email == Input.Email);

            if (exists)
            {
                ErrorMessage = "Email already exists.";
                return Page();
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = Input.Email,
                Username = Input.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password),

                // ✅ FullName có thể NULL
                FullName = string.IsNullOrWhiteSpace(Input.FullName)
                    ? null
                    : Input.FullName.Trim(),

                // ✅ Avatar mặc định
                AvatarUrl = "/images/avatar-default.png",

                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Account/Login");
        }

        public class InputModel
        {
            [Display(Name = "Full name")]
            public string? FullName { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [MinLength(6)]
            public string Password { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [Compare(nameof(Password))]
            [Display(Name = "Confirm password")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }
    }
}
