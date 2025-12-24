namespace PROJECTHUB_ENTERPRISE.Models
{
    public class ProjectOwnerVM
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
    }
}
