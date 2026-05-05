using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROJECTHUB_ENTERPRISE.Models;

[Table("tags")]
public class TagEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("project_id")]
    public Guid ProjectId { get; set; }

    [Column("name")]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Column("color_code")]
    [MaxLength(10)]
    public string ColorCode { get; set; } = "#cccccc";

    // Navigation property (optional but helpful if needed)
    public ICollection<TaskTagEntity> TaskTags { get; set; } = new List<TaskTagEntity>();
}
