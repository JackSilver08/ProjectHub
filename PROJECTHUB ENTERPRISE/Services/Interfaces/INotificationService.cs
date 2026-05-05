using PROJECTHUB_ENTERPRISE.Models;

namespace PROJECTHUB_ENTERPRISE.Services.Interfaces;

/// <summary>
/// Service xử lý thông báo hệ thống.
/// Triggers: Assigned, Mentioned, Deadline Approaching, Status Changed.
/// </summary>
public interface INotificationService
{
    Task<List<NotificationDto>> GetByUserAsync(Guid userId);
    Task<List<NotificationDto>> GetUnreadAsync(Guid userId);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task CreateAsync(CreateNotificationRequest request);
    Task CreateMentionNotificationsAsync(Guid taskId, long commentId, string content, Guid commenterId);
    Task CreateAssignmentNotificationAsync(Guid taskId, Guid assigneeId, Guid assignedById);
    Task CreateStatusChangeNotificationAsync(Guid taskId, Models.TaskStatus oldStatus, Models.TaskStatus newStatus, Guid changedById);
    Task MarkAsReadAsync(Guid notificationId, Guid userId);
    Task MarkAllAsReadAsync(Guid userId);
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public string Message { get; set; } = "";
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateNotificationRequest
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = "";
    public string Message { get; set; } = "";
    public Guid? TaskId { get; set; }
    public long? CommentId { get; set; }
    public string? LinkUrl { get; set; }
}
