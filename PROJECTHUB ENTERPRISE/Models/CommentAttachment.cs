namespace PROJECTHUB_ENTERPRISE.Models
{
    public class CommentAttachment
    {
        public Guid Id { get; set; }

        // 🔗 FK tới Comment (BIGINT)
        public long? CommentId { get; set; } // ✅ ĐÚNG


        public CommentEntity Comment { get; set; } = null!;

        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSize { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
