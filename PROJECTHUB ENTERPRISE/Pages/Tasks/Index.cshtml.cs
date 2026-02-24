using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;
using PROJECTHUB_ENTERPRISE.Models;
using System.Security.Claims;
using TaskStatusEnum = PROJECTHUB_ENTERPRISE.Models.TaskStatus;

namespace PROJECTHUB_ENTERPRISE.Pages.Tasks
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;

        public IndexModel(AppDbContext db)
        {
            _db = db;
        }

        // Filters
        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? ProjectId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Status { get; set; }

        // Data for UI
        public List<ProjectOptionVM> Projects { get; set; } = new();
        public List<TaskRowVM> Tasks { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdClaim)) return;
            if (!Guid.TryParse(userIdClaim, out var userId)) return;

            // 1) Lấy list project user tham gia + role
            var memberships = await _db.ProjectMembers
                .AsNoTracking()
                .Where(pm => pm.UserId == userId)
                .Select(pm => new { pm.ProjectId, pm.Role })
                .ToListAsync();

            var projectIds = memberships.Select(x => x.ProjectId).Distinct().ToList();

            // Options dropdown
            Projects = await _db.Projects
                .AsNoTracking()
                .Where(p => projectIds.Contains(p.Id))
                .OrderBy(p => p.Name)
                .Select(p => new ProjectOptionVM { ProjectId = p.Id, Name = p.Name })
                .ToListAsync();

            // Map role theo ProjectId để set CanManage
            var roleMap = memberships
                .GroupBy(x => x.ProjectId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Role).FirstOrDefault() ?? "");

            // 2) Query tasks thuộc các project này
            // Lưu ý: các property (t.Id, t.Title, t.AssigneeId, t.Deadline, t.Status, t.ProjectId) phải khớp TaskEntity của bạn.
            var query =
                from t in _db.Tasks.AsNoTracking()
                join p in _db.Projects.AsNoTracking() on t.ProjectId equals p.Id
                where projectIds.Contains(t.ProjectId)
                select new TaskRowVM
                {
                    TaskId = t.Id,
                    Title = t.Title,
                    ProjectId = p.Id,
                    ProjectName = p.Name,
                    AssigneeId = t.AssigneeId,
                    AssigneeName = "", // nếu muốn join Users thì làm dưới
                    Status = t.Status,
                    Deadline = t.Deadline
                };

            if (!string.IsNullOrWhiteSpace(Q))
                query = query.Where(x => x.Title.Contains(Q));

            if (ProjectId.HasValue)
                query = query.Where(x => x.ProjectId == ProjectId.Value);

            if (Status.HasValue)
                query = query.Where(x => (int)x.Status == Status.Value);

            Tasks = await query
                .OrderBy(x => x.Deadline ?? DateTime.MaxValue)
                .ThenBy(x => x.Title)
                .ToListAsync();

            // 3) Set quyền theo role project
            foreach (var t in Tasks)
            {
                t.CanManage = roleMap.TryGetValue(t.ProjectId, out var role) && role == "Manager";
            }

            // 4) Nếu muốn hiển thị AssigneeName: join Users theo AssigneeId
            // (Chỉ chạy sau khi có Tasks để tránh query phức tạp)
            var assigneeIds = Tasks.Where(x => x.AssigneeId.HasValue).Select(x => x.AssigneeId!.Value).Distinct().ToList();
            if (assigneeIds.Count > 0)
            {
                var userMap = await _db.Users.AsNoTracking()
                    .Where(u => assigneeIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.FullName);

                foreach (var t in Tasks)
                {
                    if (t.AssigneeId.HasValue && userMap.TryGetValue(t.AssigneeId.Value, out var fullName))
                        t.AssigneeName = fullName;
                }
            }
        }

        // DTOs + handlers (Create/Edit/Delete/ChangeStatus)
        public class ChangeStatusDto { public Guid TaskId { get; set; } public int Status { get; set; } }
        public class DeleteTaskDto { public Guid TaskId { get; set; } }
        public class EditTaskDto
        {
            public Guid TaskId { get; set; }
            public Guid ProjectId { get; set; }
            public string Title { get; set; } = "";
            public Guid? AssigneeId { get; set; }
            public DateTime? Deadline { get; set; }
            public int Status { get; set; }
        }
        public class CreateTaskDto
        {
            public Guid ProjectId { get; set; }
            public string Title { get; set; } = "";
            public Guid? AssigneeId { get; set; }
            public DateTime? Deadline { get; set; }
        }

        private async Task<bool> IsManager(Guid userId, Guid projectId)
        {
            return await _db.ProjectMembers.AnyAsync(pm =>
                pm.UserId == userId && pm.ProjectId == projectId && pm.Role == "Manager");
        }

        public async Task<IActionResult> OnPostChangeStatusAsync([FromBody] ChangeStatusDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return new JsonResult(new { success = false });

            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == dto.TaskId);
            if (task == null)
                return new JsonResult(new { success = false, message = "Task not found" });

            // nếu muốn giới hạn: chỉ Manager của project mới được đổi status
            // if (!await IsManager(userId, task.ProjectId)) return new JsonResult(new { success = false, message = "Forbidden" });

            task.Status = (TaskStatusEnum)dto.Status;
            task.UpdatedAt = DateTime.UtcNow; // nếu TaskEntity có UpdatedAt

            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromBody] DeleteTaskDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return new JsonResult(new { success = false });

            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == dto.TaskId);
            if (task == null)
                return new JsonResult(new { success = false, message = "Task not found" });

            if (!await IsManager(userId, task.ProjectId))
                return new JsonResult(new { success = false, message = "Forbidden" });

            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostEditAsync([FromBody] EditTaskDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return new JsonResult(new { success = false });

            if (!await IsManager(userId, dto.ProjectId))
                return new JsonResult(new { success = false, message = "Forbidden" });

            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == dto.TaskId);
            if (task == null)
                return new JsonResult(new { success = false, message = "Task not found" });

            task.Title = dto.Title;
            task.AssigneeId = dto.AssigneeId;
            task.Deadline = dto.Deadline;
            task.Status = (TaskStatusEnum)dto.Status;
            task.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostCreateAsync([FromBody] CreateTaskDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return new JsonResult(new { success = false });

            if (!await IsManager(userId, dto.ProjectId))
                return new JsonResult(new { success = false, message = "Forbidden" });

            var task = new TaskEntity
            {
                Id = Guid.NewGuid(),
                ProjectId = dto.ProjectId,
                Title = dto.Title,
                AssigneeId = dto.AssigneeId,
                Deadline = dto.Deadline,
                Status = TaskStatusEnum.Todo,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        // ViewModels
        public class ProjectOptionVM
        {
            public Guid ProjectId { get; set; }
            public string Name { get; set; } = "";
        }

        public class TaskRowVM
        {
            public Guid TaskId { get; set; }
            public string Title { get; set; } = "";
            public Guid ProjectId { get; set; }
            public string ProjectName { get; set; } = "";

            public Guid? AssigneeId { get; set; }
            public string? AssigneeName { get; set; }

            public TaskStatusEnum Status { get; set; }
            public DateTime? Deadline { get; set; }

            public bool CanManage { get; set; }
        }
    }
}
