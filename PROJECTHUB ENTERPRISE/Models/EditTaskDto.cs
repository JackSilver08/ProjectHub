namespace PROJECTHUB_ENTERPRISE.Models
{
    public class EditTaskDto
    {
        public Guid TaskId { get; set; }
        public string Title { get; set; } = "";
        public Guid? AssigneeId { get; set; }
        public DateTime? Deadline { get; set; }
        public TaskStatus Status { get; set; }
    }
}
