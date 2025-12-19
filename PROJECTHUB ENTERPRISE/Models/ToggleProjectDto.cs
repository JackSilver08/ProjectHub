namespace PROJECTHUB_ENTERPRISE.Models
{
    public class ToggleProjectDto
    {
        public Guid ProjectId { get; set; }
        public bool Archive { get; set; }
    }
}
