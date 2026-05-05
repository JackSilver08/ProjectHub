using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PROJECTHUB_ENTERPRISE.Services.Interfaces;
using System.Security.Claims;
using TaskStatusEnum = PROJECTHUB_ENTERPRISE.Models.TaskStatus;

namespace PROJECTHUB_ENTERPRISE.Pages.Tasks
{
    public class IndexModel : PageModel
    {
        private readonly ITaskService _taskService;
        private readonly IProjectService _projectService;

        public IndexModel(ITaskService taskService, IProjectService projectService)
        {
            _taskService = taskService;
            _projectService = projectService;
        }

        [BindProperty(SupportsGet = true)] public string? Q { get; set; }
        [BindProperty(SupportsGet = true)] public Guid? ProjectId { get; set; }
        [BindProperty(SupportsGet = true)] public int? Status { get; set; }

        public List<TaskRowVM> Tasks { get; set; } = new();
        public List<ProjectDropdownItem> Projects { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = GetUserIdOrDefault();
            if (userId == null) return;

            var tasks = await _taskService.GetUserTasksAsync(userId.Value, ProjectId, Status, Q);
            Tasks = tasks.Select(t => new TaskRowVM
            {
                TaskId = t.Id, Title = t.Title, ProjectId = t.ProjectId,
                ProjectName = t.ProjectName, AssigneeId = t.AssigneeId,
                AssigneeName = t.AssigneeName, Status = t.Status,
                Deadline = t.Deadline, CanManage = t.CanManage
            }).ToList();

            var userProjects = await _projectService.GetUserProjectsAsync(userId.Value);
            Projects = userProjects.Select(p => new ProjectDropdownItem { ProjectId = p.ProjectId, Name = p.Name }).ToList();
        }

        // DTOs
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

        public async Task<IActionResult> OnPostChangeStatusAsync([FromBody] ChangeStatusDto dto)
        {
            var userId = GetUserIdOrDefault();
            if (userId == null) return new JsonResult(new { success = false });
            var result = await _taskService.ChangeStatusAsync(dto.TaskId, (TaskStatusEnum)dto.Status, userId.Value);
            return new JsonResult(new { success = result.Success, error = result.Error });
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromBody] DeleteTaskDto dto)
        {
            var userId = GetUserIdOrDefault();
            if (userId == null) return new JsonResult(new { success = false });
            var result = await _taskService.DeleteAsync(dto.TaskId, userId.Value);
            return new JsonResult(new { success = result });
        }

        public async Task<IActionResult> OnPostEditAsync([FromBody] EditTaskDto dto)
        {
            var userId = GetUserIdOrDefault();
            if (userId == null) return new JsonResult(new { success = false });
            var result = await _taskService.UpdateAsync(new UpdateTaskRequest
            {
                TaskId = dto.TaskId, Title = dto.Title,
                AssigneeId = dto.AssigneeId, Deadline = dto.Deadline,
                Status = (TaskStatusEnum)dto.Status
            }, userId.Value);
            return new JsonResult(new { success = result });
        }

        public async Task<IActionResult> OnPostCreateAsync([FromBody] CreateTaskDto dto)
        {
            var userId = GetUserIdOrDefault();
            if (userId == null) return new JsonResult(new { success = false });
            await _taskService.CreateAsync(new CreateTaskRequest
            {
                ProjectId = dto.ProjectId, Title = dto.Title,
                AssigneeId = dto.AssigneeId, Deadline = dto.Deadline
            }, userId.Value);
            return new JsonResult(new { success = true });
        }

        private Guid? GetUserIdOrDefault()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out var id) ? id : null;
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
        public class ProjectDropdownItem
        {
            public Guid ProjectId { get; set; }
            public string Name { get; set; } = "";
        }
    }
}
