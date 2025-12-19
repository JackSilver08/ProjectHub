using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;
using PROJECTHUB_ENTERPRISE.Models;
using TaskStatusEnum = PROJECTHUB_ENTERPRISE.Models.TaskStatus;
using PROJECTHUB_ENTERPRISE.Dtos;

using System.Security.Claims;

namespace PROJECTHUB_ENTERPRISE.Pages.Projects
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _db;

        public DetailsModel(AppDbContext db)
        {
            _db = db;
        }
        public ProjectBoardVM Board { get; set; } = new();

        public ProjectDetailsVM Project { get; set; } = new();
        public TaskStatusEnum Status { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var role = await _db.ProjectMembers
                .Where(pm => pm.ProjectId == id && pm.UserId == userId)
                .Select(pm => pm.Role)
                .FirstOrDefaultAsync();

            if (role == null)
                return Forbid();

            var project = await _db.Projects.FirstAsync(p => p.Id == id);

            var tasks = _db.Tasks.Where(t => t.ProjectId == id);

            Project = new ProjectDetailsVM
            {
                ProjectId = id,
                Name = project.Name,
                Description = project.Description,
                IsArchived = project.IsArchived,
                CurrentUserRole = role,

                TotalTasks = await tasks.CountAsync(),
                OpenTasks = await tasks.CountAsync(t => t.Status != Models.TaskStatus.Completed),
                CompletedTasks = await tasks.CountAsync(t => t.Status == Models.TaskStatus.Completed),

                Members = await (
                    from pm in _db.ProjectMembers
                    join u in _db.Users on pm.UserId equals u.Id
                    where pm.ProjectId == id
                    select new ProjectMemberVM
                    {
                        UserId = u.Id,
                        FullName = u.FullName,
                        Email = u.Email,
                        Role = pm.Role
                    }
                ).ToListAsync()
            };
            var boardTasks = await (
    from t in _db.Tasks
    join u in _db.Users on t.AssigneeId equals u.Id into au
    from assignee in au.DefaultIfEmpty()
    where t.ProjectId == id
    select new TaskBoardItemVM
    {
        Id = t.Id,
        Title = t.Title,
        Status = t.Status,
        Priority = t.Priority,
        Deadline = t.Deadline,
        AssigneeName = assignee != null ? assignee.FullName : null
    }
).ToListAsync();
            Board.Todo = boardTasks
     .Where(t => t.Status == TaskStatusEnum.Todo)
     .ToList();

            Board.InProgress = boardTasks
                .Where(t => t.Status == TaskStatusEnum.InProgress)
                .ToList();

            Board.Review = boardTasks
                .Where(t => t.Status == TaskStatusEnum.Review)
                .ToList();

            Board.Done = boardTasks
                .Where(t => t.Status == TaskStatusEnum.Completed)
                .ToList();

            return Page();
        }

        // 🔥 ADD MEMBER
        public async Task<IActionResult> OnPostAddMemberAsync([FromBody] AddMemberRequest req)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Check quyền
            var role = await _db.ProjectMembers
                .Where(pm => pm.ProjectId == req.ProjectId && pm.UserId == userId)
                .Select(pm => pm.Role)
                .FirstOrDefaultAsync();

            if (role != "Manager")
                return Forbid();

            var exists = await _db.ProjectMembers.AnyAsync(pm =>
                pm.ProjectId == req.ProjectId &&
                pm.UserId == req.UserId);

            if (exists)
                return BadRequest("User already in project");

            _db.ProjectMembers.Add(new ProjectMemberEntity
            {
                ProjectId = req.ProjectId,
                UserId = req.UserId,
                Role = "Member"
            });

            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        // Remove Member
        public async Task<IActionResult> OnPostRemoveMemberAsync(Guid projectId, Guid userId)
        {
            var currentUserId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            // 🔐 Check quyền Manager
            var role = await _db.ProjectMembers
                .Where(pm => pm.ProjectId == projectId && pm.UserId == currentUserId)
                .Select(pm => pm.Role)
                .FirstOrDefaultAsync();

            if (role != "Manager")
                return Forbid();

            // ❗ Chỉ lấy đúng 1 member cần xóa
            var member = await _db.ProjectMembers
                .FirstOrDefaultAsync(pm =>
                    pm.ProjectId == projectId &&
                    pm.UserId == userId);

            if (member == null)
                return NotFound();

            _db.ProjectMembers.Remove(member);
            await _db.SaveChangesAsync();

            return RedirectToPage(new { id = projectId });
        }
        public async Task<IActionResult> OnPostCreateTaskAsync([FromBody] CreateTaskDto dto)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            var task = new TaskEntity
            {
                Id = Guid.NewGuid(),
                ProjectId = dto.ProjectId,
                Title = dto.Title,
                CreatorId = userId,
                AssigneeId = dto.AssigneeId,
                Deadline = dto.Deadline.HasValue
    ? DateTime.SpecifyKind(dto.Deadline.Value, DateTimeKind.Utc)
    : null,

                Status = Models.TaskStatus.Todo,
                Priority = 0,
                CreatedAt = DateTime.UtcNow
            };

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeleteTaskAsync(
    [FromBody] DeleteTaskDto dto)
        {
            var task = await _db.Tasks
                .FirstOrDefaultAsync(t => t.Id == dto.TaskId);

            if (task == null)
                return NotFound();

            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }
        public async Task<IActionResult> OnPostEditTaskAsync(
    [FromBody] EditTaskDto dto)
        {
            var task = await _db.Tasks
                .FirstOrDefaultAsync(t => t.Id == dto.TaskId);

            if (task == null)
                return NotFound();

            task.Title = dto.Title;
            task.AssigneeId = dto.AssigneeId;
            task.Status = dto.Status;
            task.UpdatedAt = DateTime.UtcNow;

            task.Deadline = dto.Deadline.HasValue
                ? DateTime.SpecifyKind(dto.Deadline.Value, DateTimeKind.Utc)
                : null;

            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }
    }
}
