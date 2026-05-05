using PROJECTHUB_ENTERPRISE.Models;

namespace PROJECTHUB_ENTERPRISE.Services.Interfaces;

/// <summary>
/// Service xử lý logic nghiệp vụ cho Project module.
/// </summary>
public interface IProjectService
{
    // ── QUERY ──────────────────────────────────────────
    Task<List<ProjectSummaryDto>> GetUserProjectsAsync(Guid userId, string? search = null, bool? archived = null);
    Task<List<ProjectSummaryDto>> GetArchivedProjectsAsync(Guid userId, string? search = null);
    Task<ProjectEntity?> GetByIdAsync(Guid projectId);
    Task<string?> GetUserRoleAsync(Guid projectId, Guid userId);
    Task<bool> IsManagerAsync(Guid projectId, Guid userId);
    Task<bool> IsOwnerAsync(Guid projectId, Guid userId);

    // ── CRUD ───────────────────────────────────────────
    Task<ProjectEntity> CreateAsync(string name, string? description, Guid managerId);
    Task<bool> UpdateAsync(Guid projectId, string name, string? description, Guid userId);
    Task<bool> ArchiveAsync(Guid projectId, Guid userId);
    Task<bool> RestoreAsync(Guid projectId, Guid userId);
    Task<bool> DeleteAsync(Guid projectId, Guid userId);

    // ── MEMBERS ────────────────────────────────────────
    Task<List<ProjectMemberInfo>> GetMembersAsync(Guid projectId);
    Task<bool> AddMemberAsync(Guid projectId, Guid userId, Guid addedByUserId, string role = "Member");
    Task<bool> RemoveMemberAsync(Guid projectId, Guid userId, Guid removedByUserId);

    // ── PROGRESS ───────────────────────────────────────
    /// <summary>
    /// Tính % tiến độ dự án dựa trên các Task có ContributesToProgress = true.
    /// Công thức: (Done tasks / (Total - Cancelled)) * 100
    /// </summary>
    Task<ProjectProgressInfo> CalculateProgressAsync(Guid projectId);

    // ── TAGS ───────────────────────────────────────────
    Task<List<Models.TagEntity>> GetTagsAsync(Guid projectId);
    Task<Models.TagEntity> CreateTagAsync(Guid projectId, string name, string colorCode);
    Task<bool> DeleteTagAsync(Guid tagId, Guid userId);
}

// ── DTOs cho Project Service ──────────────────────────

public class ProjectSummaryDto
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Role { get; set; } = "";
    public string Status { get; set; } = "";
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public double ProgressPercent { get; set; }
}

public class ProjectMemberInfo
{
    public Guid UserId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = "";
    public DateTime JoinedAt { get; set; }
}

public class ProjectProgressInfo
{
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OpenTasks { get; set; }
    public int CancelledTasks { get; set; }
    public double ProgressPercent { get; set; }

    // Chart breakdown
    public int Todo { get; set; }
    public int InProgress { get; set; }
    public int Review { get; set; }
    public int OnHold { get; set; }
}

