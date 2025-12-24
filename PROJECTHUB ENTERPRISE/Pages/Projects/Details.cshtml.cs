using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;
using PROJECTHUB_ENTERPRISE.Dtos;
using PROJECTHUB_ENTERPRISE.Hubs;
using PROJECTHUB_ENTERPRISE.Models;
using PROJECTHUB_ENTERPRISE.ViewModels;
using System.ComponentModel.Design;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TaskStatusEnum = PROJECTHUB_ENTERPRISE.Models.TaskStatus;


namespace PROJECTHUB_ENTERPRISE.Pages.Projects
{
    public class DetailsModel : PageModel
    {
        private readonly IHubContext<ProjectHub> _hub;

        private readonly AppDbContext _db;

      
        public ProjectBoardVM Board { get; set; } = new();

        public ProjectDetailsVM Project { get; set; } = new();
        public TaskStatusEnum Status { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var owner = await (
    from pm in _db.ProjectMembers
    join u in _db.Users on pm.UserId equals u.Id
    where pm.ProjectId == id && pm.Role == "Manager"
    select new ProjectOwnerVM
    {
        UserId = u.Id,
        FullName = u.FullName,
        Email = u.Email
    }
).FirstAsync();
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
                Owner = owner,

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

            Project.Wikis = await _db.WikiPages
.Where(w => w.ProjectId == id)
.OrderBy(w => w.Title)
.ToListAsync();
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
        private async Task<bool> IsAdminAsync(Guid projectId)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            return await _db.ProjectMembers.AnyAsync(pm =>
                pm.ProjectId == projectId &&
                pm.UserId == userId &&
                pm.Role == "Manager"   // hoặc "Admin"
            );
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
            await BroadcastProjectSummary(dto.ProjectId);
            return new JsonResult(new { success = true });

        }
        public DetailsModel(
         AppDbContext db,
         IHubContext<ProjectHub> hub)
        {
            _db = db;
            _hub = hub;
        }
        private async Task BroadcastProjectSummary(Guid projectId)
        {
            var tasks = _db.Tasks.Where(t => t.ProjectId == projectId);

            var todo = await tasks.CountAsync(t => t.Status == TaskStatusEnum.Todo);
            var inProgress = await tasks.CountAsync(t => t.Status == TaskStatusEnum.InProgress);
            var review = await tasks.CountAsync(t => t.Status == TaskStatusEnum.Review);
            var completed = await tasks.CountAsync(t => t.Status == TaskStatusEnum.Completed);

            await _hub.Clients
                .Group(projectId.ToString())
                .SendAsync("ProjectStatsUpdated", new
                {
                    summary = new
                    {
                        totalTasks = todo + inProgress + review + completed,
                        openTasks = todo + inProgress + review,
                        completedTasks = completed
                    },
                    chart = new
                    {
                        todo,
                        inProgress,
                        review,
                        completed
                    }
                });
        }
        public async Task<IActionResult> OnPostDeleteTaskAsync(Guid id)
        {
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
                return NotFound();

            if (!await IsAdminAsync(task.ProjectId))
                return Forbid();

            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();
            await BroadcastProjectSummary(task.ProjectId);
            return new JsonResult(new { success = true });

        }

        public async Task<IActionResult> OnPostEditTaskAsync(
     [FromBody] EditTaskDto dto)
        {
            var task = await _db.Tasks
                .FirstOrDefaultAsync(t => t.Id == dto.TaskId);

            if (task == null)
                return NotFound();

            if (!await IsAdminAsync(task.ProjectId))
                return Forbid();

            task.Title = dto.Title;
            task.AssigneeId = dto.AssigneeId;
            task.Status = dto.Status;
            task.UpdatedAt = DateTime.UtcNow;

            task.Deadline = dto.Deadline.HasValue
                ? DateTime.SpecifyKind(dto.Deadline.Value, DateTimeKind.Utc)
                : null;

            await _db.SaveChangesAsync();
            await BroadcastProjectSummary(task.ProjectId);
            return new JsonResult(new { success = true });

        }
        private TaskStatusEnum GetNextStatus(TaskStatusEnum current)
        {
            return current switch
            {
                TaskStatusEnum.Todo => TaskStatusEnum.InProgress,
                TaskStatusEnum.InProgress => TaskStatusEnum.Review,
                TaskStatusEnum.Review => TaskStatusEnum.Completed,
                TaskStatusEnum.Completed => TaskStatusEnum.Todo,
                _ => TaskStatusEnum.Todo
            };
        }
        public async Task<IActionResult> OnPostChangeTaskStatusAsync(
     [FromBody] ChangeTaskStatusDto dto)
        {
            var task = await _db.Tasks
                .FirstOrDefaultAsync(t => t.Id == dto.TaskId);

            if (task == null)
                return NotFound();

            if (!await IsAdminAsync(task.ProjectId))
                return Forbid();

            // ✅ VALIDATE ENUM
            if (!Enum.IsDefined(typeof(TaskStatusEnum), dto.Status))
                return BadRequest("Invalid status");

            task.Status = (TaskStatusEnum)dto.Status;
            task.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await BroadcastProjectSummary(task.ProjectId);

            return new JsonResult(new
            {
                success = true,
                status = task.Status.ToString()
            });
        }

        public async Task<IActionResult> OnPostCycleTaskStatusAsync(Guid taskId)
        {
            var task = await _db.Tasks
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return NotFound();

            if (!await IsAdminAsync(task.ProjectId))
                return Forbid(); // 🔥 CHẶN USER THƯỜNG

            task.Status = GetNextStatus(task.Status);
            task.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                status = task.Status.ToString()
            });
        }
        public async Task<IActionResult> OnGetWiki(Guid projectId)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            var role = await _db.ProjectMembers
                .Where(p => p.ProjectId == projectId && p.UserId == userId)
                .Select(p => p.Role)
                .FirstOrDefaultAsync();

            ViewData["CanEdit"] = role == "Manager";

            var wikis = await _db.WikiPages
                .Where(w => w.ProjectId == projectId)
                .OrderBy(w => w.Title)
                .ToListAsync();

            return Partial("_WikiPartial", wikis);
        }

        private async Task<bool> IsProjectOwner(Guid projectId)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            return await _db.Projects
                .AnyAsync(p => p.Id == projectId && p.ManagerId == userId);
        }


        public async Task<IActionResult> OnGetWikiDetailAsync(Guid id)
        {
            var wiki = await _db.WikiPages.FindAsync(id);
            if (wiki == null) return NotFound();

            return new JsonResult(new
            {
                id = wiki.Id,
                title = wiki.Title,
                content = wiki.Content
            });
        }
     

public async Task<IActionResult> OnPostCreateWikiAsync(
    [FromBody] WikiCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!await IsProjectOwner(dto.ProjectId))
            return Forbid();

        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var wiki = new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = dto.ProjectId,
            Title = dto.Title,
            Content = dto.Content,
            UpdatedAt = DateTime.UtcNow,
            LastUpdatedBy = userId
        };

        _db.WikiPages.Add(wiki);
        await _db.SaveChangesAsync();

        return new JsonResult(new { success = true });
    }

        public async Task<IActionResult> OnPostEditWikiAsync(
         [FromBody] WikiEditDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var wiki = await _db.WikiPages.FindAsync(dto.Id);
            if (wiki == null)
                return NotFound();

            if (!await IsProjectOwner(wiki.ProjectId))
                return Forbid();

            wiki.Title = dto.Title;
            wiki.Content = dto.Content;
            wiki.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeleteWikiAsync(Guid id)
        {
            var wiki = await _db.WikiPages.FindAsync(id);
            if (wiki == null) return NotFound();

            if (!await IsProjectOwner(wiki.ProjectId))
                return Forbid();

            _db.WikiPages.Remove(wiki);
            await _db.SaveChangesAsync();

            return new JsonResult(true);
        }

        public async Task<IActionResult> OnPostDeleteProjectAsync(
    [FromBody] DeleteProjectRequest req)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            var project = await _db.Projects
                .FirstOrDefaultAsync(p => p.Id == req.ProjectId);

            if (project == null)
                return new JsonResult(new { success = false });

            if (project.ManagerId != userId)
                return new JsonResult(new { success = false, message = "Forbidden" });

            _db.Projects.Remove(project);
            await _db.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                redirectUrl = Url.Page("/Index")
            });
        }


        // Comment 
        public async Task<IActionResult> OnGetTaskCommentsAsync(Guid taskId)
        {
            // 1️⃣ Load comments
            var comments = await _db.Comments
    .Where(c => c.TaskId == taskId && !c.IsDeleted)
    .OrderBy(c => c.CreatedAt)
    .Select(c => new CommentItemVM
    {
        Id = c.Id,
        TaskId = c.TaskId,
        ParentId = c.ParentId,
        UserId = c.UserId,
        Content = c.Content,
        CreatedAt = c.CreatedAt,

        Attachments = c.Attachments.Select(a => new CommentAttachmentVM
        {
            FileName = a.FileName,
            FilePath = a.FilePath,
            ContentType = a.ContentType,
            FileSize = a.FileSize
        }).ToList(),

        Replies = new List<CommentItemVM>()
    })
    .ToListAsync();


            // 2️⃣ Lấy danh sách userId (long)
            var userIds = comments
                .Select(c => c.UserId)
                .Distinct()
                .ToList();

            // ⚠️ BẠN PHẢI CÓ CỘT map
            // VD: Users.CommentUserId (long) hoặc tương tự
            var users = await _db.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);


            // 3️⃣ Map user info
            foreach (var c in comments)
            {
                if (users.TryGetValue(c.UserId, out var u))
                {
                    c.UserName = u.FullName;
                    c.AvatarUrl = u.AvatarUrl;
                }
            }

            // 4️⃣ Build tree
            var lookup = comments.ToDictionary(c => c.Id);
            var roots = new List<CommentItemVM>();

            foreach (var c in comments)
            {
                if (c.ParentId == null)
                    roots.Add(c);
                else if (lookup.TryGetValue(c.ParentId.Value, out var parent))
                    parent.Replies.Add(c);
            }

            return new JsonResult(roots);
        }

        public async Task<IActionResult> OnPostCreateCommentAsync(
    [FromForm] CreateCommentDto dto)
        {
            // Kiểm tra authentication
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized();
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            // Tạo comment với ID kiểu long
            var comment = new CommentEntity
            {
                // KHÔNG set Id - để database tự tạo (identity)
                // Id sẽ được database tự tạo vì nó là bigint identity
                TaskId = dto.TaskId,
                UserId = userId,
                Content = dto.Content,
                ParentId = dto.ParentId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync(); // Sau save, comment.Id sẽ có giá trị
                                          // ===============================
                                          // 📎 SAVE ATTACHMENTS
                                          // ===============================
            if (dto.Attachments != null && dto.Attachments.Any())
            {
                var uploadRoot = Path.Combine("wwwroot", "uploads", "comments", comment.Id.ToString());
                Directory.CreateDirectory(uploadRoot);

                foreach (var file in dto.Attachments)
                {
                    if (file.Length == 0) continue;

                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                    var filePath = Path.Combine(uploadRoot, fileName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    // 👉 Nếu bạn có bảng CommentAttachment
                    _db.CommentAttachments.Add(new CommentAttachment
                    {
                        Id = Guid.NewGuid(),
                        CommentId = comment.Id,
                        FileName = file.FileName,
                        FilePath = $"/uploads/comments/{comment.Id}/{fileName}",
                        ContentType = file.ContentType,
                        FileSize = file.Length,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _db.SaveChangesAsync();
            }
            // ===============================
            // 🔔 CREATE MENTION NOTIFICATIONS
            // ===============================

            var mentionedUserIds = ExtractMentionedUserIds(dto.Content);

            // Lấy thông tin task và commenter
            var task = await _db.Tasks
                .FirstOrDefaultAsync(t => t.Id == dto.TaskId);

            var commenter = await _db.Users.FindAsync(userId);
            var commenterName = commenter?.FullName ?? "Someone";
            var taskTitle = task?.Title ?? "a task";

            // Tạo notifications
            var notifications = new List<NotificationEntity>();

            foreach (var mentionedUserId in mentionedUserIds)
            {
                // ❌ không tự notify chính mình
                if (mentionedUserId == userId)
                    continue;

                var projectId = task?.ProjectId;

                var notification = new NotificationEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = mentionedUserId,
                    Type = "CommentMention",
                    Message = $"{commenterName} mentioned you in a comment on task: {taskTitle}",
                    TaskId = dto.TaskId,
                    CommentId = comment.Id,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    LinkUrl = $"/Projects/Details?id={projectId}&taskId={dto.TaskId}&commentId={comment.Id}"
                };



                notifications.Add(notification);
            }

            if (notifications.Any())
            {
                _db.Notifications.AddRange(notifications);
                await _db.SaveChangesAsync();
            }

            return new JsonResult(new { success = true });
        }
        private List<Guid> ExtractMentionedUserIds(string content)
        {
            var result = new List<Guid>();

            // @{guid}|Full Name
            var matches = Regex.Matches(
                content,
                @"@\{([0-9a-fA-F-]{36})\}\|"
            );

            foreach (Match m in matches)
            {
                if (Guid.TryParse(m.Groups[1].Value, out var id))
                {
                    result.Add(id);
                }
            }

            return result.Distinct().ToList();
        }
        private List<string> ExtractMentions(string content)
        {
            return Regex.Matches(content, @"@(\w+)")
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();
        }
        public async Task<IActionResult> OnGetProjectMembersAsync(Guid projectId)
        {
            var members = await (
                from pm in _db.ProjectMembers
                join u in _db.Users on pm.UserId equals u.Id
                where pm.ProjectId == projectId
                select new
                {
                    u.Id,
                    u.FullName,
                    u.AvatarUrl
                }
            ).ToListAsync();

            return new JsonResult(members);
        }
    }
}
