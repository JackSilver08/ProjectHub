using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PROJECTHUB_ENTERPRISE.Services.Interfaces;
using System.Security.Claims;
using PROJECTHUB_ENTERPRISE.Extensions;

namespace PROJECTHUB_ENTERPRISE.Pages.Dashboard
{
    public class NotificationsModel : PageModel
    {
        private readonly INotificationService _notificationService;

        public List<NotificationDto> Notifications { get; set; } = new();

        public NotificationsModel(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToPage("/Account/Login");

            var userId = User.GetUserId();
            if (!userId.HasValue) return Unauthorized();

            Notifications = await _notificationService.GetByUserAsync(userId.Value);
            return Page();
        }

        public async Task<IActionResult> OnGetUnreadNotificationsAsync()
        {
            try
            {
                var userId = User.GetUserIdOrThrow();
                var unread = await _notificationService.GetUnreadAsync(userId);
                return new JsonResult(unread);
            }
            catch (UnauthorizedAccessException)
            {
                return new JsonResult(new List<object>());
            }
        }

        public async Task<IActionResult> OnPostMarkAsReadAsync([FromBody] MarkReadDto dto)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue) return Unauthorized();
            await _notificationService.MarkAsReadAsync(dto.NotificationId, userId.Value);
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostMarkAllReadAsync()
        {
            var userId = User.GetUserId();
            if (!userId.HasValue) return Unauthorized();
            await _notificationService.MarkAllAsReadAsync(userId.Value);
            return new JsonResult(new { success = true });
        }

        public class MarkReadDto { public Guid NotificationId { get; set; } }
    }
}
