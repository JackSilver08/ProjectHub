using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using PROJECTHUB_ENTERPRISE.Dtos;
using PROJECTHUB_ENTERPRISE.Hubs;
using PROJECTHUB_ENTERPRISE.Models;
using PROJECTHUB_ENTERPRISE.Services.Interfaces;
using System.Security.Claims;
using TaskStatusEnum = PROJECTHUB_ENTERPRISE.Models.TaskStatus;

namespace PROJECTHUB_ENTERPRISE.Pages.Projects
{
    public class DetailsModel : PageModel
    {
        private readonly IProjectService _projectService;
        private readonly ITaskService _taskService;
        private readonly ICommentService _commentService;
        private readonly IWikiService _wikiService;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<ProjectHub> _hub;

        public DetailsModel(
            IProjectService projectService,
            ITaskService taskService,
            ICommentService commentService,
            IWikiService wikiService,
            INotificationService notificationService,
            IHubContext<ProjectHub> hub)
        {
            _projectService = projectService;
            _taskService = taskService;
            _commentService = commentService;
            _wikiService = wikiService;
            _notificationService = notificationService;
            _hub = hub;
        }

        public ProjectDetailsVM Project { get; set; } = new();
        public ProjectBoardVM Board { get; set; } = new();
        public List<PROJECTHUB_ENTERPRISE.Models.TagEntity> Tags { get; set; } = new();

        // ── GET: Load project details page ─────────────

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var userId = GetUserId();
            var role = await _projectService.GetUserRoleAsync(id, userId);
            if (role == null) return Forbid();

            var project = await _projectService.GetByIdAsync(id);
            if (project == null) return NotFound();

            var members = await _projectService.GetMembersAsync(id);
            var progress = await _projectService.CalculateProgressAsync(id);
            var owner = members.FirstOrDefault(m => m.Role == "Manager");

            Project = new ProjectDetailsVM
            {
                ProjectId = id,
                Name = project.Name,
                Description = project.Description,
                IsArchived = project.IsArchived,
                CurrentUserRole = role,
                Owner = owner != null ? new ProjectOwnerVM
                {
                    UserId = owner.UserId,
                    FullName = owner.FullName,
                    Email = owner.Email
                } : new ProjectOwnerVM(),
                TotalTasks = progress.TotalTasks,
                OpenTasks = progress.OpenTasks,
                CompletedTasks = progress.CompletedTasks,
                ProgressPercent = progress.ProgressPercent,
                CanEditWiki = role == "Manager",
                CanEditSettings = role == "Manager",
                Members = members.Select(m => new ProjectMemberVM
                {
                    UserId = m.UserId,
                    FullName = m.FullName,
                    Email = m.Email,
                    Role = m.Role
                }).ToList()
            };

            // Build Kanban board
            var tasks = await _taskService.GetTasksByProjectAsync(id);
            Board.Todo = tasks.Where(t => t.Status == TaskStatusEnum.Todo).Select(MapToBoard).ToList();
            Board.InProgress = tasks.Where(t => t.Status == TaskStatusEnum.InProgress).Select(MapToBoard).ToList();
            Board.Review = tasks.Where(t => t.Status == TaskStatusEnum.Review).Select(MapToBoard).ToList();
            Board.Done = tasks.Where(t => t.Status == TaskStatusEnum.Completed).Select(MapToBoard).ToList();

            // Load wiki
            var wikis = await _wikiService.GetByProjectAsync(id);
            Project.Wikis = wikis.Select(w => new WikiPage
            {
                Id = w.Id, ProjectId = w.ProjectId, Title = w.Title,
                Slug = w.Slug, Content = w.Content, UpdatedAt = w.UpdatedAt
            }).ToList();

            return Page();
        }

        // ── GET: Timeline data ─────────────────────────

        public async Task<IActionResult> OnGetTimelineAsync(Guid id, string range = "30d")
        {
            var data = await _taskService.GetTimelineAsync(id, range);
            return new JsonResult(new
            {
                labels = data.Labels,
                created = data.Created,
                completed = data.Completed
            });
        }

        // ── GET: Summary stats (for SignalR refresh) ───

        public async Task<IActionResult> OnGetSummaryStatsAsync(Guid id)
        {
            var progress = await _projectService.CalculateProgressAsync(id);
            return new JsonResult(new
            {
                summary = new
                {
                    totalTasks = progress.TotalTasks,
                    openTasks = progress.OpenTasks,
                    completedTasks = progress.CompletedTasks,
                    progressPercent = progress.ProgressPercent
                },
                chart = new
                {
                    todo = progress.Todo,
                    inProgress = progress.InProgress,
                    review = progress.Review,
                    completed = progress.CompletedTasks,
                    onHold = progress.OnHold
                }
            });
        }

        // ── GET: Project members list (JSON) ───────────

        public async Task<IActionResult> OnGetProjectMembersAsync(Guid projectId)
        {
            var members = await _projectService.GetMembersAsync(projectId);
            return new JsonResult(members.Select(m => new
            {
                m.UserId, m.FullName, m.AvatarUrl
            }));
        }

        // ── POST: Add member ───────────────────────────

        public async Task<IActionResult> OnPostAddMemberAsync([FromBody] AddMemberRequest req)
        {
            var userId = GetUserId();
            var result = await _projectService.AddMemberAsync(req.ProjectId, req.UserId, userId);
            if (!result) return BadRequest("Cannot add member");
            return new JsonResult(new { success = true });
        }

        // ── POST: Remove member ────────────────────────

        public async Task<IActionResult> OnPostRemoveMemberAsync(Guid projectId, Guid userId)
        {
            var currentUserId = GetUserId();
            var result = await _projectService.RemoveMemberAsync(projectId, userId, currentUserId);
            if (!result) return Forbid();
            return RedirectToPage(new { id = projectId });
        }

        // ── POST: Create task ──────────────────────────

        public async Task<IActionResult> OnGetTaskDetailAsync(Guid taskId)
        {
            var userId = GetUserId();
            var detail = await _taskService.GetTaskDetailAsync(taskId, userId);
            if (detail == null) return NotFound();
            return new JsonResult(detail);
        }

        public async Task<IActionResult> OnPostCreateTaskAsync([FromBody] CreateTaskDto dto)
        {
            var userId = GetUserId();
            var task = await _taskService.CreateAsync(new CreateTaskRequest
            {
                ProjectId = dto.ProjectId,
                Title = dto.Title,
                Description = dto.Description,
                AssigneeId = dto.AssigneeId,
                Deadline = dto.Deadline,
                ContributesToProgress = true
            }, userId);

            await BroadcastProjectSummary(dto.ProjectId);
            return new JsonResult(new { success = true });
        }

        // ── POST: Edit task ────────────────────────────

        public async Task<IActionResult> OnPostEditTaskAsync([FromBody] EditTaskDto dto)
        {
            var userId = GetUserId();
            var result = await _taskService.UpdateAsync(new UpdateTaskRequest
            {
                TaskId = dto.TaskId,
                Title = dto.Title,
                Description = dto.Description,
                AssigneeId = dto.AssigneeId,
                Status = dto.Status,
                Deadline = dto.Deadline
            }, userId);

            if (!result) return Forbid();
            var task = await _taskService.GetByIdAsync(dto.TaskId);
            if (task != null) await BroadcastProjectSummary(task.ProjectId);
            return new JsonResult(new { success = true });
        }

        // ── POST: Delete task ──────────────────────────

        public async Task<IActionResult> OnPostDeleteTaskAsync(Guid id)
        {
            var task = await _taskService.GetByIdAsync(id);
            if (task == null) return NotFound();

            var userId = GetUserId();
            var result = await _taskService.DeleteAsync(id, userId);
            if (!result) return Forbid();

            await BroadcastProjectSummary(task.ProjectId);
            return new JsonResult(new { success = true });
        }

        // ── POST: Change task status ───────────────────

        public async Task<IActionResult> OnPostChangeTaskStatusAsync(
            [FromBody] ChangeTaskStatusDto dto)
        {
            var userId = GetUserId();
            var changeResult = await _taskService.ChangeStatusAsync(
                dto.TaskId, (TaskStatusEnum)dto.Status, userId);

            if (!changeResult.Success)
                return BadRequest(changeResult.Error);

            var task = await _taskService.GetByIdAsync(dto.TaskId);
            if (task != null) await BroadcastProjectSummary(task.ProjectId);

            return new JsonResult(new { success = true, status = changeResult.NewStatus.ToString() });
        }

        // ── POST: Cycle task status ────────────────────

        public async Task<IActionResult> OnPostCycleTaskStatusAsync(Guid taskId)
        {
            var userId = GetUserId();
            var result = await _taskService.CycleStatusAsync(taskId, userId);
            if (!result.Success) return Forbid();

            return new JsonResult(new { success = true, status = result.NewStatus.ToString() });
        }

        // ── Wiki endpoints ─────────────────────────────

        public async Task<IActionResult> OnGetWiki(Guid projectId)
        {
            var userId = GetUserId();
            var role = await _projectService.GetUserRoleAsync(projectId, userId);
            ViewData["CanEdit"] = role == "Manager";

            var wikis = await _wikiService.GetByProjectAsync(projectId);
            var wikiPages = wikis.Select(w => new WikiPage
            {
                Id = w.Id, ProjectId = w.ProjectId, Title = w.Title,
                Slug = w.Slug, Content = w.Content, UpdatedAt = w.UpdatedAt
            }).ToList();
            return Partial("_WikiPartial", wikiPages);
        }

        public async Task<IActionResult> OnGetWikiDetailAsync(Guid id)
        {
            var wiki = await _wikiService.GetByIdAsync(id);
            if (wiki == null) return NotFound();
            return new JsonResult(new { id = wiki.Id, title = wiki.Title, content = wiki.Content });
        }

        public async Task<IActionResult> OnPostCreateWikiAsync([FromBody] WikiCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = GetUserId();
            if (!await _projectService.IsOwnerAsync(dto.ProjectId, userId)) return Forbid();
            await _wikiService.CreateAsync(dto.ProjectId, dto.Title, dto.Content, userId);
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostEditWikiAsync([FromBody] WikiEditDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = GetUserId();
            var wiki = await _wikiService.GetByIdAsync(dto.Id);
            if (wiki == null) return NotFound();
            if (!await _projectService.IsOwnerAsync(wiki.ProjectId, userId)) return Forbid();
            await _wikiService.UpdateAsync(dto.Id, dto.Title, dto.Content, userId);
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeleteWikiAsync(Guid id)
        {
            var userId = GetUserId();
            var wiki = await _wikiService.GetByIdAsync(id);
            if (wiki == null) return NotFound();
            if (!await _projectService.IsOwnerAsync(wiki.ProjectId, userId)) return Forbid();
            await _wikiService.DeleteAsync(id, userId);
            return new JsonResult(true);
        }

        // ── POST: Delete project ───────────────────────

        public async Task<IActionResult> OnPostDeleteProjectAsync([FromBody] DeleteProjectRequest req)
        {
            var userId = GetUserId();
            var result = await _projectService.DeleteAsync(req.ProjectId, userId);
            if (!result)
                return new JsonResult(new { success = false, message = "Forbidden" });
            return new JsonResult(new { success = true, redirectUrl = Url.Page("/Index") });
        }

        // ── Comment endpoints ──────────────────────────

        public async Task<IActionResult> OnGetTaskCommentsAsync(Guid taskId)
        {
            var userId = User.Identity?.IsAuthenticated == true ? GetUserId() : (Guid?)null;
            var roots = await _commentService.GetCommentTreeAsync(taskId, userId);
            return new JsonResult(roots);
        }

        public async Task<IActionResult> OnPostCreateCommentAsync([FromForm] CreateCommentDto dto)
        {
            if (!User.Identity?.IsAuthenticated ?? true) return Unauthorized();
            var userId = GetUserId();

                        await _commentService.CreateAsync(new CreateCommentRequest
            {
                TaskId = dto.TaskId,
                Content = dto.Content,
                ParentId = dto.ParentId,
                Attachments = dto.Attachments?.ToList()
            }, userId);

            // Fetch the task to get ProjectId for broadcasting
            var taskDetail = await _taskService.GetTaskDetailAsync(dto.TaskId, userId);
            if (taskDetail != null)
            {
                await _hub.Clients.Group(taskDetail.ProjectId.ToString())
                    .SendAsync("CommentAdded", new { taskId = dto.TaskId });
            }

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostVoteCommentAsync([FromBody] VoteCommentRequest req)
        {
            if (!User.Identity?.IsAuthenticated ?? true) return Unauthorized();
            var userId = GetUserId();

                        var result = await _commentService.VoteAsync(req.CommentId, userId, req.IsUpvote);
            var taskDetail = await _taskService.GetTaskDetailAsync(req.TaskId, userId);
            if (taskDetail != null)
            {
                await _hub.Clients.Group(taskDetail.ProjectId.ToString())
                    .SendAsync("CommentVoted", new { taskId = req.TaskId });
            }
            return new JsonResult(new { success = true, upvotes = result.Upvotes, downvotes = result.Downvotes });
        }

        public class VoteCommentRequest
        {
            public long CommentId { get; set; }
            public bool IsUpvote { get; set; }
            public Guid TaskId { get; set; }
        }

        // ── Private helpers ────────────────────────────

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task BroadcastProjectSummary(Guid projectId)
        {
            var progress = await _projectService.CalculateProgressAsync(projectId);
            await _hub.Clients.Group(projectId.ToString())
                .SendAsync("ProjectStatsUpdated", new
                {
                    summary = new
                    {
                        totalTasks = progress.TotalTasks,
                        openTasks = progress.OpenTasks,
                        completedTasks = progress.CompletedTasks,
                        progressPercent = progress.ProgressPercent
                    },
                    chart = new
                    {
                        todo = progress.Todo,
                        inProgress = progress.InProgress,
                        review = progress.Review,
                        completed = progress.CompletedTasks,
                        onHold = progress.OnHold
                    }
                });
        }

        private static TaskBoardItemVM MapToBoard(TaskItemDto t) => new()
        {
            Id = t.Id, Title = t.Title, Status = t.Status,
            Priority = t.Priority, Deadline = t.Deadline,
            AssigneeId = t.AssigneeId, AssigneeName = t.AssigneeName, Tags = t.Tags
        };
    }

    // ── ViewModels (kept for Razor views compatibility) ─

    public class ProjectDetailsVM
    {
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public bool IsArchived { get; set; }
        public string CurrentUserRole { get; set; } = "";
        public ProjectOwnerVM Owner { get; set; } = new();
        public int TotalTasks { get; set; }
        public int OpenTasks { get; set; }
        public int CompletedTasks { get; set; }
        public double ProgressPercent { get; set; }
        public bool CanEditWiki { get; set; }
        public bool CanEditSettings { get; set; }
        public List<ProjectMemberVM> Members { get; set; } = new();
        public List<WikiPage> Wikis { get; set; } = new();
    }

    public class ProjectBoardVM
    {
        public List<TaskBoardItemVM> Todo { get; set; } = new();
        public List<TaskBoardItemVM> InProgress { get; set; } = new();
        public List<TaskBoardItemVM> Review { get; set; } = new();
        public List<TaskBoardItemVM> Done { get; set; } = new();
    }

    public class TaskBoardItemVM
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public TaskStatusEnum Status { get; set; }
        public int Priority { get; set; }
        public DateTime? Deadline { get; set; }
        public Guid? AssigneeId { get; set; }
        public string? AssigneeName { get; set; }
        public List<PROJECTHUB_ENTERPRISE.Services.Interfaces.TagDto> Tags { get; set; } = new();
    }

    public class ProjectOwnerVM
    {
        public Guid UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
    }

    public class ProjectMemberVM
    {
        public Guid UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string Role { get; set; } = "";
    }
}








