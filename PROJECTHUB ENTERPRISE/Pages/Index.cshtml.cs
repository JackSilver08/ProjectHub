using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PROJECTHUB_ENTERPRISE.Models;
using PROJECTHUB_ENTERPRISE.Services.Interfaces;
using System.Security.Claims;

public class IndexModel : PageModel
{
    private readonly IProjectService _projectService;

    public IndexModel(IProjectService projectService)
    {
        _projectService = projectService;
    }

    public DashboardStatsVM Stats { get; set; } = new();
    public List<ProjectRowVM> Projects { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return;
        var userId = Guid.Parse(userIdClaim);

        var projects = await _projectService.GetUserProjectsAsync(userId);

        Projects = projects.Select(p => new ProjectRowVM
        {
            ProjectId = p.ProjectId,
            Name = p.Name,
            Role = p.Role,
            Status = p.Status
        }).ToList();

        Stats.TotalProjects = Projects.Count;
        Stats.OpenTasks = projects.Sum(p => p.TotalTasks - p.CompletedTasks);
        Stats.CompletedTasks = projects.Sum(p => p.CompletedTasks);
    }

    public async Task<IActionResult> OnPostDeleteAsync([FromBody] DeleteProjectDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return new JsonResult(new { success = false });
        var userId = Guid.Parse(userIdClaim);
        var result = await _projectService.ArchiveAsync(dto.ProjectId, userId);
        return new JsonResult(new { success = result });
    }

    public async Task<IActionResult> OnPostEditAsync([FromBody] EditProjectDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _projectService.UpdateAsync(dto.ProjectId, dto.Name, dto.Description, userId);
        return new JsonResult(new { success = result });
    }

    public async Task<IActionResult> OnPostToggleAsync([FromBody] ToggleProjectDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = dto.Archive
            ? await _projectService.ArchiveAsync(dto.ProjectId, userId)
            : await _projectService.RestoreAsync(dto.ProjectId, userId);
        return new JsonResult(new { success = result });
    }
}

public class DashboardStatsVM
{
    public int TotalProjects { get; set; }
    public int OpenTasks { get; set; }
    public int CompletedTasks { get; set; }
}

public class ProjectRowVM
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string Status { get; set; } = "";
}
