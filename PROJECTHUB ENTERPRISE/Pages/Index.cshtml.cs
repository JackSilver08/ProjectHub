using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;
using PROJECTHUB_ENTERPRISE.Models;
using System.Security.Claims;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }
 

    public DashboardStatsVM Stats { get; set; } = new();
    public List<ProjectRowVM> Projects { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return;

        var userId = Guid.Parse(userIdClaim);

        // 1️⃣ PROJECT LIST
        Projects = await (
            from pm in _db.ProjectMembers
            join p in _db.Projects on pm.ProjectId equals p.Id
            where pm.UserId == userId
            select new ProjectRowVM
            {
                ProjectId = p.Id,
                Name = p.Name,
                Role = pm.Role,
                Status = p.IsArchived ? "Archived" : "Active"
            }
        ).ToListAsync();

        // 2️⃣ TOTAL PROJECTS
        Stats.TotalProjects = Projects.Count;

        // 3️⃣ TASK COUNTS (task thuộc project mà user tham gia)
        var projectIds = Projects.Select(p => p.ProjectId).ToList();


        Stats.OpenTasks = await _db.Tasks
    .CountAsync(t =>
        projectIds.Contains(t.ProjectId) &&
        t.Status != PROJECTHUB_ENTERPRISE.Models.TaskStatus.Completed);

        Stats.CompletedTasks = await _db.Tasks
            .CountAsync(t =>
                projectIds.Contains(t.ProjectId) &&
                t.Status == PROJECTHUB_ENTERPRISE.Models.TaskStatus.Completed);


    }

    public async Task<IActionResult> OnPostDeleteAsync([FromBody] DeleteProjectDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return new JsonResult(new { success = false });

        var userId = Guid.Parse(userIdClaim);

        // Check role
        var member = await _db.ProjectMembers
            .FirstOrDefaultAsync(pm =>
                pm.ProjectId == dto.ProjectId &&
                pm.UserId == userId &&
                pm.Role == "Manager");

        if (member == null)
            return new JsonResult(new
            {
                success = false,
                message = "You are not allowed to delete this project"
            });

        var project = await _db.Projects
            .FirstOrDefaultAsync(p => p.Id == dto.ProjectId);

        if (project == null)
            return new JsonResult(new { success = false });

        // 👉 SOFT DELETE (khuyên dùng)
        project.IsArchived = true;

        await _db.SaveChangesAsync();

        return new JsonResult(new { success = true });
    }
    public async Task<IActionResult> OnPostEditAsync([FromBody] EditProjectDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var project = await _db.Projects
            .FirstOrDefaultAsync(p => p.Id == dto.ProjectId);

        if (project == null) return new JsonResult(new { success = false });

        project.Name = dto.Name;
        project.Description = dto.Description;
        project.IsArchived = dto.IsArchived;
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostToggleAsync([FromBody] ToggleProjectDto dto)
    {
        var project = await _db.Projects
            .FirstOrDefaultAsync(p => p.Id == dto.ProjectId);

        if (project == null)
            return new JsonResult(new { success = false });

        project.IsArchived = dto.Archive;
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return new JsonResult(new { success = true });
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
