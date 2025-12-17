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

    }
}
