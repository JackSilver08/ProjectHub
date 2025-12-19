namespace PROJECTHUB_ENTERPRISE.Models
{
    public class ProjectDetailsVM
    {
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsArchived { get; set; }
        public string CurrentUserRole { get; set; } = "";

        public int TotalTasks { get; set; }
        public int OpenTasks { get; set; }
        public int CompletedTasks { get; set; }

        public List<ProjectMemberVM> Members { get; set; } = new();
    }

  
}
