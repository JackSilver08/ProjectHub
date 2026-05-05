using System.ComponentModel.DataAnnotations.Schema;

namespace PROJECTHUB_ENTERPRISE.Models;

[Table("task_tags")]
public class TaskTagEntity
{
    [Column("task_id")]
    public Guid TaskId { get; set; }

    [Column("tag_id")]
    public Guid TagId { get; set; }

    // Navigation properties
    public TaskEntity Task { get; set; } = null!;
    public TagEntity Tag { get; set; } = null!;
}
