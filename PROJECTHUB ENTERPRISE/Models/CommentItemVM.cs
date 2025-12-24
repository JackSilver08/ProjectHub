namespace PROJECTHUB_ENTERPRISE.Models
{
    public class CommentItemVM
    {
        public long Id { get; set; }
        public Guid TaskId { get; set; }
        public long? ParentId { get; set; }

        public Guid UserId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }

        // 👤 user info
        public string UserName { get; set; }
        public string AvatarUrl { get; set; }

        // 📎 FILES — 🔥 BẮT BUỘC PHẢI CÓ
        public List<CommentAttachmentVM> Attachments { get; set; }
            = new();

        // 💬 replies
        public List<CommentItemVM> Replies { get; set; }
            = new();
    }
}
