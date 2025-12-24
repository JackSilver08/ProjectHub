using System.ComponentModel.DataAnnotations.Schema;

namespace PROJECTHUB_ENTERPRISE.Models
{
    [Table("comments")]
    public class CommentEntity
    {
        public long Id { get; set; }          // bigint
        public Guid TaskId { get; set; }       // uuid
        public Guid UserId { get; set; }       // uuid
        public string Content { get; set; } = null!;
        public long? ParentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }

        // NAVIGATION
        public User User { get; set; } = null!;
        public TaskEntity Task { get; set; } = null!;

        public CommentEntity? Parent { get; set; }
        public ICollection<CommentEntity> Replies { get; set; }
            = new List<CommentEntity>();

        // ✅ FIX LỖI
        public ICollection<CommentAttachment> Attachments { get; set; } = new List<CommentAttachment>();

    }

}
