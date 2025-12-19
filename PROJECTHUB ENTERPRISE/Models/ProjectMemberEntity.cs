using System.ComponentModel.DataAnnotations.Schema;

[Table("project_members")]
public class ProjectMemberEntity
{
    [Column("project_id")]
    public Guid ProjectId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("role")]
    public string Role { get; set; } = "Member";

    [Column("joined_at")]
    public DateTime JoinedAt { get; set; }
}
