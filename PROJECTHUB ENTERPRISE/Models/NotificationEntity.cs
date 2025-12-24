namespace PROJECTHUB_ENTERPRISE.Models
{
    // Trong file NotificationEntity.cs
    public class NotificationEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Type { get; set; } = "";
        public string Message { get; set; } = "";

        // CÁCH 1: Nếu là Guid?
        public Guid? TaskId { get; set; }
        public long? CommentId { get; set; }

        // CÁCH 2: Nếu là Guid (không nullable)
        // public Guid TaskId { get; set; }
        // public long CommentId { get; set; }

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? LinkUrl { get; set; }
    }
}
