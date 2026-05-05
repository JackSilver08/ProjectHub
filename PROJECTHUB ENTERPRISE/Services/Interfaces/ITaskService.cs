using PROJECTHUB_ENTERPRISE.Models;

namespace PROJECTHUB_ENTERPRISE.Services.Interfaces;

/// <summary>
/// Service xử lý logic nghiệp vụ cho Task module.
/// Bao gồm CRUD, workflow chuyển trạng thái, và quy tắc phân quyền.
/// </summary>
public interface ITaskService
{
    // ── QUERY ──────────────────────────────────────────
    Task<List<TaskItemDto>> GetTasksByProjectAsync(Guid projectId);
    Task<List<TaskItemDto>> GetUserTasksAsync(Guid userId, Guid? projectId = null, int? status = null, string? search = null);
    Task<TaskDetailDto?> GetTaskDetailAsync(Guid taskId, Guid requestingUserId);
    Task<TaskEntity?> GetByIdAsync(Guid taskId);

    // ── CRUD ───────────────────────────────────────────
    Task<TaskEntity> CreateAsync(CreateTaskRequest request, Guid creatorId);
    Task<bool> UpdateAsync(UpdateTaskRequest request, Guid userId);
    Task<bool> DeleteAsync(Guid taskId, Guid userId);

    // ── WORKFLOW ───────────────────────────────────────
    /// <summary>
    /// Chuyển trạng thái Task theo quy tắc:
    /// - Member: chỉ New → InProgress → Review
    /// - Manager: bất kỳ trạng thái nào
    /// </summary>
    Task<TaskStatusChangeResult> ChangeStatusAsync(Guid taskId, Models.TaskStatus newStatus, Guid userId);

    /// <summary>
    /// Cycle qua trạng thái tiếp theo (dùng cho nút quick-action).
    /// </summary>
    Task<TaskStatusChangeResult> CycleStatusAsync(Guid taskId, Guid userId);

    // ── TIMELINE ───────────────────────────────────────
    Task<TaskTimelineData> GetTimelineAsync(Guid projectId, string range = "30d");

    // ── TAGS ───────────────────────────────────────────
    Task<bool> AddTagAsync(Guid taskId, Guid tagId, Guid userId);
    Task<bool> RemoveTagAsync(Guid taskId, Guid tagId, Guid userId);
    Task<bool> SyncTagsAsync(Guid taskId, List<Guid> tagIds, Guid userId);
}

// ── DTOs ──────────────────────────────────────────────

public class TaskItemDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public Models.TaskStatus Status { get; set; }
    public int Priority { get; set; }
    public DateTime? Deadline { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsPinned { get; set; }
    public bool ContributesToProgress { get; set; }
    public Guid? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public string? AssigneeAvatar { get; set; }
    public Guid CreatorId { get; set; }
    public string? CreatorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool CanManage { get; set; }
    public List<TagDto> Tags { get; set; } = new();
}

public class TagDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string ColorCode { get; set; } = "";
}

public class TaskDetailDto : TaskItemDto
{
    public List<CommentItemDto> Comments { get; set; } = new();
    public int CommentCount { get; set; }
}

public class CommentItemDto
{
    public long Id { get; set; }
    public Guid TaskId { get; set; }
    public long? ParentId { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int Upvotes { get; set; }
    public int Downvotes { get; set; }
    public bool? CurrentUserVote { get; set; } // true: upvoted, false: downvoted, null: not voted
    public List<AttachmentDto> Attachments { get; set; } = new();
    public List<CommentItemDto> Replies { get; set; } = new();
}

public class AttachmentDto
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string? ContentType { get; set; }
    public long FileSize { get; set; }
}

public class CreateTaskRequest
{
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public Guid? AssigneeId { get; set; }
    public DateTime? Deadline { get; set; }
    public int Priority { get; set; }
    public bool IsPrivate { get; set; }
    public bool ContributesToProgress { get; set; } = true;
}

public class UpdateTaskRequest
{
    public Guid TaskId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public Guid? AssigneeId { get; set; }
    public DateTime? Deadline { get; set; }
    public Models.TaskStatus Status { get; set; }
    public int Priority { get; set; }
    public bool IsPrivate { get; set; }
    public bool ContributesToProgress { get; set; } = true;
}

public class TaskStatusChangeResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public Models.TaskStatus NewStatus { get; set; }
}

public class TaskTimelineData
{
    public List<string> Labels { get; set; } = new();
    public int[] Created { get; set; } = Array.Empty<int>();
    public int[] Completed { get; set; } = Array.Empty<int>();
}

