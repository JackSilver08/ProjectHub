using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PROJECTHUB_ENTERPRISE.Services.Interfaces;
using System.Security.Claims;

namespace PROJECTHUB_ENTERPRISE.Pages.Projects
{
    public class ArchiveModel : PageModel
    {
        private readonly IProjectService _projectService;

        public ArchiveModel(IProjectService projectService)
        {
            _projectService = projectService;
        }

        public List<ProjectRowVM> Projects { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        public async Task OnGetAsync()
        {
            var userId = GetUserIdOrDefault();
            if (userId == null) return;

            var projects = await _projectService.GetArchivedProjectsAsync(userId.Value, Q);
            Projects = projects.Select(p => new ProjectRowVM
            {
                ProjectId = p.ProjectId,
                Name = p.Name,
                Role = p.Role
            }).ToList();
        }

        public class ProjectIdDto { public Guid ProjectId { get; set; } }

        public async Task<IActionResult> OnPostRestoreAsync([FromBody] ProjectIdDto dto)
        {
            var userId = GetUserIdOrDefault();
            if (userId == null) return new JsonResult(new { success = false });
            var result = await _projectService.RestoreAsync(dto.ProjectId, userId.Value);
            return new JsonResult(new { success = result });
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromBody] ProjectIdDto dto)
        {
            var userId = GetUserIdOrDefault();
            if (userId == null) return new JsonResult(new { success = false });
            var result = await _projectService.DeleteAsync(dto.ProjectId, userId.Value);
            return new JsonResult(new { success = result });
        }

        private Guid? GetUserIdOrDefault()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out var id) ? id : null;
        }

        public class ProjectRowVM
        {
            public Guid ProjectId { get; set; }
            public string Name { get; set; } = "";
            public string Role { get; set; } = "";
        }
    }
}
