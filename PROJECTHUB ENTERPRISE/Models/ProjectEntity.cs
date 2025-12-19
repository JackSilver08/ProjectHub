using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("projects")]
public class ProjectEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("manager_id")]
    public Guid ManagerId { get; set; }

    [Column("is_archived")]
    public bool IsArchived { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // ✅ CHUẨN: nullable + map đúng DB
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

}
