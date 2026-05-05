using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROJECTHUB_ENTERPRISE.Models;

[Table("tasks")]
public class TaskEntity
{
    [Key] // 👈 BẮT BUỘC
    [Column("id")]
    public Guid Id { get; set; }

    [Column("project_id")]
    public Guid ProjectId { get; set; }

    [Column("title")]
    public string Title { get; set; } = "";

    [Column("description")]
    public string? Description { get; set; }

    [Column("creator_id")]
    public Guid CreatorId { get; set; }

    [Column("assignee_id")]
    public Guid? AssigneeId { get; set; }

    [Column("status")]
    public TaskStatus Status { get; set; }

    [Column("priority")]
    public int Priority { get; set; }

    [Column("deadline")]
    public DateTime? Deadline { get; set; }

    [Column("is_private")]
    public bool IsPrivate { get; set; }

    [Column("is_pinned")]
    public bool IsPinned { get; set; }

    /// <summary>
    /// Cờ đánh dấu task này có tính vào % tiến độ dự án không.
    /// True = công việc thực tế (Design UI, Code API) → khi Done sẽ tăng %.
    /// False = thảo luận, hành chính (Họp team, Brainstorm) → không ảnh hưởng %.
    /// Mặc định: true.
    /// </summary>
    [Column("contributes_to_progress")]
    public bool ContributesToProgress { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
