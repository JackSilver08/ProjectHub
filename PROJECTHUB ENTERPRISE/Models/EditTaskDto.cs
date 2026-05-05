namespace PROJECTHUB_ENTERPRISE.Models
{
    public class EditTaskDto
    {
        public Guid TaskId { get; set; }
        public string Title { get; set; }
    public string? Description { get; set; }
    public List<Guid>? TagIds { get; set; } = new();
        public Guid? AssigneeId { get; set; }
        public DateTime? Deadline { get; set; }
        public TaskStatus Status { get; set; }
    }
}


