using System.ComponentModel.DataAnnotations.Schema;

namespace PROJECTHUB_ENTERPRISE.Models;

[Table("comment_votes")]
public class VoteEntity
{
    [Column("comment_id")]
    public long CommentId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("is_upvote")]
    public bool IsUpvote { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
