using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;
using PROJECTHUB_ENTERPRISE.Models;
using System.Security.Claims;
using PROJECTHUB_ENTERPRISE.Extensions;
namespace PROJECTHUB_ENTERPRISE.Pages.Dashboard
{
    public class NotificationsModel : PageModel
    {
        private readonly AppDbContext _db;

        // 👇 DỮ LIỆU TRUYỀN RA VIEW
        public List<NotificationEntity> Notifications { get; set; } = new();

        // 👇 CONSTRUCTOR INJECT DB
        public NotificationsModel(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Kiểm tra người dùng đã đăng nhập chưa
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToPage("/Account/Login");
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                // Thử lấy từ Name nếu NameIdentifier không có
                userIdClaim = User.Identity?.Name;
            }

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            Notifications = await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return Page();
        }
        public async Task<IActionResult> OnGetUnreadNotificationsAsync()
        {
            try
            {
                // Sử dụng extension method GetUserIdOrThrow
                var userId = User.GetUserIdOrThrow();

                var unread = await _db.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new
                    {
                        n.Id,
                        n.Message,
                        n.LinkUrl,
                        n.CreatedAt
                    })
                    .ToListAsync();

                return new JsonResult(unread);
            }
            catch (UnauthorizedAccessException)
            {
                // Return empty array if user is not authenticated
                return new JsonResult(new List<object>());
            }
            catch (Exception ex)
            {
                // Log error and return empty
                Console.WriteLine($"Error getting unread notifications: {ex.Message}");
                return new JsonResult(new List<object>());
            }
        }

    }
}
