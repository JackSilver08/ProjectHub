using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;
using System.Security.Claims;

namespace PROJECTHUB_ENTERPRISE.Pages.Projects
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;

        public IndexModel(AppDbContext db)
        {
            _db = db;
        }

        public List<ProjectRowVM> Projects { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? Archived { get; set; }

        public async Task OnGetAsync()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdClaim)) return;

            if (!Guid.TryParse(userIdClaim, out var userId)) return;

            var query =
                from pm in _db.ProjectMembers.AsNoTracking()
                join p in _db.Projects.AsNoTracking() on pm.ProjectId equals p.Id
                where pm.UserId == userId
                select new ProjectRowVM
                {
                    ProjectId = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Role = pm.Role,
                    Status = p.IsArchived ? "Archived" : "Active"
                };

            if (!string.IsNullOrWhiteSpace(Q))
                query = query.Where(x => x.Name.Contains(Q));

            if (Archived.HasValue)
            {
                var isArchived = Archived.Value;
                query = query.Where(x => (x.Status == "Archived") == isArchived);
                // Hoặc tốt hơn nếu bạn map IsArchived trực tiếp vào VM:
                // query = query.Where(x => x.IsArchived == isArchived);
            }

            Projects = await query.OrderBy(x => x.Name).ToListAsync();
        }

        // DTOs
        public class DeleteProjectDto
        {
            public Guid ProjectId { get; set; }
        }

        public class EditProjectDto
        {
            public Guid ProjectId { get; set; }
            public string Name { get; set; } = "";
            public string? Description { get; set; }
            public bool IsArchived { get; set; }
        }

        public class ToggleProjectDto
        {
            public Guid ProjectId { get; set; }
            public bool Archive { get; set; }
        }

        // Handlers
        public async Task<IActionResult> OnPostDeleteAsync([FromBody] DeleteProjectDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return new JsonResult(new { success = false });

            var member = await _db.ProjectMembers
                .FirstOrDefaultAsync(pm =>
                    pm.ProjectId == dto.ProjectId &&
                    pm.UserId == userId &&
                    pm.Role == "Manager");

            if (member == null)
                return new JsonResult(new { success = false, message = "You are not allowed to delete this project" });

            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == dto.ProjectId);
            if (project == null)
                return new JsonResult(new { success = false, message = "Project not found" });

            // Soft delete
            project.IsArchived = true;
            project.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostEditAsync([FromBody] EditProjectDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return new JsonResult(new { success = false });

            // Nên check quyền Manager giống Delete/Toggle để tránh user thường sửa bậy
            var member = await _db.ProjectMembers
                .FirstOrDefaultAsync(pm =>
                    pm.ProjectId == dto.ProjectId &&
                    pm.UserId == userId &&
                    pm.Role == "Manager");

            if (member == null)
                return new JsonResult(new { success = false, message = "You are not allowed to edit this project" });

            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == dto.ProjectId);
            if (project == null)
                return new JsonResult(new { success = false, message = "Project not found" });

            project.Name = dto.Name?.Trim() ?? project.Name;
            project.Description = dto.Description?.Trim();
            project.IsArchived = dto.IsArchived;
            project.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostToggleAsync([FromBody] ToggleProjectDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return new JsonResult(new { success = false });

            var member = await _db.ProjectMembers
                .FirstOrDefaultAsync(pm =>
                    pm.ProjectId == dto.ProjectId &&
                    pm.UserId == userId &&
                    pm.Role == "Manager");

            if (member == null)
                return new JsonResult(new { success = false, message = "You are not allowed to change status" });

            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == dto.ProjectId);
            if (project == null)
                return new JsonResult(new { success = false, message = "Project not found" });

            project.IsArchived = dto.Archive;
            project.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
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
