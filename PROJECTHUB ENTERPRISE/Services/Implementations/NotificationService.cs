using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;
using PROJECTHUB_ENTERPRISE.Models;
using PROJECTHUB_ENTERPRISE.Services.Interfaces;
using System.Text.RegularExpressions;
using TaskStatusEnum = PROJECTHUB_ENTERPRISE.Models.TaskStatus;

namespace PROJECTHUB_ENTERPRISE.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    public NotificationService(AppDbContext db) { _db = db; }

    public async Task<List<NotificationDto>> GetByUserAsync(Guid userId)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id, Type = n.Type, Message = n.Message,
                LinkUrl = n.LinkUrl, IsRead = n.IsRead, CreatedAt = n.CreatedAt
            }).ToListAsync();
    }

    public async Task<List<NotificationDto>> GetUnreadAsync(Guid userId)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id, Type = n.Type, Message = n.Message,
                LinkUrl = n.LinkUrl, IsRead = n.IsRead, CreatedAt = n.CreatedAt
            }).ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task CreateAsync(CreateNotificationRequest request)
    {
        _db.Notifications.Add(new NotificationEntity
        {
            Id = Guid.NewGuid(), UserId = request.UserId,
            Type = request.Type, Message = request.Message,
            TaskId = request.TaskId, CommentId = request.CommentId,
            LinkUrl = request.LinkUrl, IsRead = false, CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task CreateMentionNotificationsAsync(
        Guid taskId, long commentId, string content, Guid commenterId)
    {
        var mentionedIds = ExtractMentionedUserIds(content);
        if (!mentionedIds.Any()) return;

        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        var commenter = await _db.Users.FindAsync(commenterId);
        var commenterName = commenter?.FullName ?? "Someone";
        var taskTitle = task?.Title ?? "a task";

        var notifications = new List<NotificationEntity>();
        foreach (var uid in mentionedIds)
        {
            if (uid == commenterId) continue; // Không tự notify
            notifications.Add(new NotificationEntity
            {
                Id = Guid.NewGuid(), UserId = uid, Type = "CommentMention",
                Message = $"{commenterName} mentioned you in a comment on task: {taskTitle}",
                TaskId = taskId, CommentId = commentId, IsRead = false,
                CreatedAt = DateTime.UtcNow,
                LinkUrl = $"/Projects/Details?id={task?.ProjectId}&taskId={taskId}&commentId={commentId}"
            });
        }
        if (notifications.Any())
        {
            _db.Notifications.AddRange(notifications);
            await _db.SaveChangesAsync();
        }
    }

    public async Task CreateAssignmentNotificationAsync(Guid taskId, Guid assigneeId, Guid assignedById)
    {
        if (assigneeId == assignedById) return;
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        var assigner = await _db.Users.FindAsync(assignedById);
        await CreateAsync(new CreateNotificationRequest
        {
            UserId = assigneeId, Type = "Assigned",
            Message = $"{assigner?.FullName ?? "Someone"} assigned you to task: {task?.Title}",
            TaskId = taskId,
            LinkUrl = $"/Projects/Details?id={task?.ProjectId}&taskId={taskId}"
        });
    }

    public async Task CreateStatusChangeNotificationAsync(
        Guid taskId, TaskStatusEnum oldStatus, TaskStatusEnum newStatus, Guid changedById)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null) return;
        var changer = await _db.Users.FindAsync(changedById);

        // Notify creator and assignee (except the person who changed it)
        var notifyIds = new HashSet<Guid>();
        if (task.CreatorId != changedById) notifyIds.Add(task.CreatorId);
        if (task.AssigneeId.HasValue && task.AssigneeId.Value != changedById)
            notifyIds.Add(task.AssigneeId.Value);

        foreach (var uid in notifyIds)
        {
            await CreateAsync(new CreateNotificationRequest
            {
                UserId = uid, Type = "StatusChanged",
                Message = $"{changer?.FullName ?? "Someone"} changed task '{task.Title}' from {oldStatus} to {newStatus}",
                TaskId = taskId,
                LinkUrl = $"/Projects/Details?id={task.ProjectId}&taskId={taskId}"
            });
        }
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId);
        if (n != null) { n.IsRead = true; await _db.SaveChangesAsync(); }
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync();
    }

    private List<Guid> ExtractMentionedUserIds(string content)
    {
        var result = new List<Guid>();
        var matches = Regex.Matches(content, @"@\{([0-9a-fA-F-]{36})\}\|");
        foreach (Match m in matches)
            if (Guid.TryParse(m.Groups[1].Value, out var id)) result.Add(id);
        return result.Distinct().ToList();
    }
}
