namespace PROJECTHUB_ENTERPRISE.Models
{
    public class ProjectMemberVM
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
    }
}
