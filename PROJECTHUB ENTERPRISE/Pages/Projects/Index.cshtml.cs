using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PROJECTHUB_ENTERPRISE.Services.Interfaces;
using System.Security.Claims;

namespace PROJECTHUB_ENTERPRISE.Pages.Projects
{
    public class IndexModel : PageModel
    {
        private readonly IProjectService _projectService;

        public IndexModel(IProjectService projectService)
        {
            _projectService = projectService;
        }

        public List<ProjectRowVM> Projects { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? Archived { get; set; }

        public async Task OnGetAsync()
        {
            var userId = GetUserIdOrDefault();
            if (userId == null) return;

            var projects = await _projectService.GetUserProjectsAsync(userId.Value, Q, Archived);
            Projects = projects.Select(p => new ProjectRowVM
            {
                ProjectId = p.ProjectId,
                Name = p.Name,
                Description = p.Description,
                Role = p.Role,
                Status = p.Status
            }).ToList();
        }

        // DTOs
        public class DeleteProjectDto { public Guid ProjectId { get; set; } }
        public class EditProjectDto
        {
            public Guid ProjectId { get; set; }
            public string Name { get; set; } = "";
            public string? Description { get; set; }
            public bool IsArchived { get; set; }
        }
        public class ToggleProjectDto { public Guid ProjectId { get; set; } public bool Archive { get; set; } }

        public async Task<IActionResult> OnPostDeleteAsync([FromBody] DeleteProjectDto dto)
        {
            var userId = GetUserIdOrDefault();
            if (userId == null) return new JsonResult(new { success = false });
            var result = await _projectService.ArchiveAsync(dto.ProjectId, userId.Value);
            return new JsonResult(new { success = result });
        }

        public async Task<IActionResult> OnPostEditAsync([FromBody] EditProjectDto dto)
        {
            var userId = GetUserIdOrDefault();
            if (userId == null) return new JsonResult(new { success = false });
            var result = await _projectService.UpdateAsync(dto.ProjectId, dto.Name, dto.Description, userId.Value);
            return new JsonResult(new { success = result });
        }

        public async Task<IActionResult> OnPostToggleAsync([FromBody] ToggleProjectDto dto)
        {
            var userId = GetUserIdOrDefault();
            if (userId == null) return new JsonResult(new { success = false });
            var result = dto.Archive
                ? await _projectService.ArchiveAsync(dto.ProjectId, userId.Value)
                : await _projectService.RestoreAsync(dto.ProjectId, userId.Value);
            return new JsonResult(new { success = result });
        }

        private Guid? GetUserIdOrDefault()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public class ProjectRowVM
    {
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string Role { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
