using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;
using System.Security.Claims;

namespace PROJECTHUB_ENTERPRISE.Pages.Projects
{
    public class ArchiveModel : PageModel
    {
        private readonly AppDbContext _db;

        public ArchiveModel(AppDbContext db)
        {
            _db = db;
        }

        public List<ProjectRowVM> Projects { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        public async Task OnGetAsync()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdClaim)) return;
            if (!Guid.TryParse(userIdClaim, out var userId)) return;

            var query =
                from pm in _db.ProjectMembers.AsNoTracking()
                join p in _db.Projects.AsNoTracking() on pm.ProjectId equals p.Id
                where pm.UserId == userId && p.IsArchived == true
                select new ProjectRowVM
                {
                    ProjectId = p.Id,
                    Name = p.Name,
                    Role = pm.Role
                };

            if (!string.IsNullOrWhiteSpace(Q))
                query = query.Where(x => x.Name.Contains(Q));

            Projects = await query.OrderBy(x => x.Name).ToListAsync();
        }

        // DTO
        public class ProjectIdDto
        {
            public Guid ProjectId { get; set; }
        }

        public async Task<IActionResult> OnPostRestoreAsync([FromBody] ProjectIdDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return new JsonResult(new { success = false });

            // Manager check
            var member = await _db.ProjectMembers.FirstOrDefaultAsync(pm =>
                pm.ProjectId == dto.ProjectId &&
                pm.UserId == userId &&
                pm.Role == "Manager");

            if (member == null)
                return new JsonResult(new { success = false, message = "You are not allowed to restore this project" });

            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == dto.ProjectId);
            if (project == null)
                return new JsonResult(new { success = false, message = "Project not found" });

            project.IsArchived = false;
            project.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromBody] ProjectIdDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return new JsonResult(new { success = false });

            // Manager check
            var member = await _db.ProjectMembers.FirstOrDefaultAsync(pm =>
                pm.ProjectId == dto.ProjectId &&
                pm.UserId == userId &&
                pm.Role == "Manager");

            if (member == null)
                return new JsonResult(new { success = false, message = "You are not allowed to delete this project" });

            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == dto.ProjectId);
            if (project == null)
                return new JsonResult(new { success = false, message = "Project not found" });

            // Nếu bạn đang dùng soft delete theo nghĩa “archive”, thì ở trang archive thường không cần xóa cứng.
            // Nếu vẫn muốn xóa cứng:
            _db.Projects.Remove(project);

            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public class ProjectRowVM
        {
            public Guid ProjectId { get; set; }
            public string Name { get; set; } = "";
            public string Role { get; set; } = "";
        }
    }
}
