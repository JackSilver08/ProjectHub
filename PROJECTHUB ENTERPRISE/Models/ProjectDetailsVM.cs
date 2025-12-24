using PROJECTHUB_ENTERPRISE.Models;

namespace PROJECTHUB_ENTERPRISE.ViewModels
{
    public class ProjectDetailsVM
    {
        /* ===== PROJECT INFO ===== */
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsArchived { get; set; }

        /* ===== OWNER ===== */
        public ProjectOwnerVM Owner { get; set; } = new();

        public bool CanEditSettings => CurrentUserRole == "Manager";

        /* ===== CURRENT USER ===== */
        public string CurrentUserRole { get; set; } = "";
        public bool CanEditProject => CurrentUserRole == "Manager";

        /* ===== TASK SUMMARY ===== */
        public int TotalTasks { get; set; }
        public int OpenTasks { get; set; }
        public int CompletedTasks { get; set; }

        /* ===== MEMBERS ===== */
        public List<ProjectMemberVM> Members { get; set; } = new();

        /* ===== WIKI ===== */
        public List<WikiPage> Wikis { get; set; } = new();
        public bool CanEditWiki =>
            CurrentUserRole == "Manager" || CurrentUserRole == "Member";
    }
}
