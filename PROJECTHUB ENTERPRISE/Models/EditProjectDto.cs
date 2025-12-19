namespace PROJECTHUB_ENTERPRISE.Models
{
    public class EditProjectDto
    {
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public bool IsArchived { get; set; }
    }
}
