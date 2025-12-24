namespace PROJECTHUB_ENTERPRISE.Models
{
    public class CreateCommentDto
    {
        public Guid TaskId { get; set; }   // ✅ FIX
        public string Content { get; set; } = "";
        public long? ParentId { get; set; }
        public List<IFormFile>? Attachments { get; set; }

    }

}
