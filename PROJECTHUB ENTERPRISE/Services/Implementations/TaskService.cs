using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;
using PROJECTHUB_ENTERPRISE.Models;
using PROJECTHUB_ENTERPRISE.Services.Interfaces;
using TaskStatusEnum = PROJECTHUB_ENTERPRISE.Models.TaskStatus;

namespace PROJECTHUB_ENTERPRISE.Services.Implementations;

public class TaskService : ITaskService
{
    private readonly AppDbContext _db;
    public TaskService(AppDbContext db) { _db = db; }

    public async Task<List<TaskItemDto>> GetTasksByProjectAsync(Guid projectId)
    {
        var tasks = await (
            from t in _db.Tasks.AsNoTracking()
            join u in _db.Users.AsNoTracking() on t.AssigneeId equals u.Id into au
            from assignee in au.DefaultIfEmpty()
            where t.ProjectId == projectId
            orderby t.IsPinned descending, t.CreatedAt descending
            select new TaskItemDto
            {
                Id = t.Id, ProjectId = t.ProjectId, Title = t.Title,
                Description = t.Description, Status = t.Status, Priority = t.Priority,
                Deadline = t.Deadline, IsPrivate = t.IsPrivate, IsPinned = t.IsPinned,
                ContributesToProgress = t.ContributesToProgress,
                AssigneeId = t.AssigneeId,
                AssigneeName = assignee != null ? assignee.FullName : null,
                CreatorId = t.CreatorId, CreatedAt = t.CreatedAt
            }).ToListAsync();

        var taskIds = tasks.Select(t => t.Id).ToList();
        var tags = await _db.TaskTags.AsNoTracking()
            .Where(tt => taskIds.Contains(tt.TaskId))
            .Join(_db.Tags.AsNoTracking(), tt => tt.TagId, t => t.Id, (tt, t) => new { tt.TaskId, Tag = t })
            .ToListAsync();

        foreach (var t in tasks)
        {
            t.Tags = tags.Where(x => x.TaskId == t.Id).Select(x => new TagDto
            {
                Id = x.Tag.Id, Name = x.Tag.Name, ColorCode = x.Tag.ColorCode
            }).ToList();
        }

        return tasks;
    }

    public async Task<List<TaskItemDto>> GetUserTasksAsync(
        Guid userId, Guid? projectId = null, int? status = null, string? search = null)
    {
        var memberships = await _db.ProjectMembers.AsNoTracking()
            .Where(pm => pm.UserId == userId)
            .Select(pm => new { pm.ProjectId, pm.Role }).ToListAsync();
        var projectIds = memberships.Select(x => x.ProjectId).Distinct().ToList();
        var roleMap = memberships.GroupBy(x => x.ProjectId)
            .ToDictionary(g => g.Key, g => g.First().Role);

        var query = from t in _db.Tasks.AsNoTracking()
            join p in _db.Projects.AsNoTracking() on t.ProjectId equals p.Id
            where projectIds.Contains(t.ProjectId)
            select new TaskItemDto
            {
                Id = t.Id, ProjectId = p.Id, ProjectName = p.Name,
                Title = t.Title, Status = t.Status, Priority = t.Priority,
                Deadline = t.Deadline, AssigneeId = t.AssigneeId,
                CreatorId = t.CreatorId, CreatedAt = t.CreatedAt
            };

        if (projectId.HasValue) query = query.Where(x => x.ProjectId == projectId.Value);
        if (status.HasValue) query = query.Where(x => (int)x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Title.Contains(search));

        var tasks = await query.OrderBy(x => x.Deadline ?? DateTime.MaxValue).ThenBy(x => x.Title).ToListAsync();

        var assigneeIds = tasks.Where(x => x.AssigneeId.HasValue).Select(x => x.AssigneeId!.Value).Distinct().ToList();
        if (assigneeIds.Count > 0)
        {
            var userMap = await _db.Users.AsNoTracking()
                .Where(u => assigneeIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName);
            foreach (var t in tasks)
                if (t.AssigneeId.HasValue && userMap.TryGetValue(t.AssigneeId.Value, out var name))
                    t.AssigneeName = name;
        }
        foreach (var t in tasks)
            t.CanManage = roleMap.TryGetValue(t.ProjectId, out var role) && role == "Manager";

        var taskIds = tasks.Select(t => t.Id).ToList();
        var tags = await _db.TaskTags.AsNoTracking()
            .Where(tt => taskIds.Contains(tt.TaskId))
            .Join(_db.Tags.AsNoTracking(), tt => tt.TagId, t => t.Id, (tt, t) => new { tt.TaskId, Tag = t })
            .ToListAsync();

        foreach (var t in tasks)
        {
            t.Tags = tags.Where(x => x.TaskId == t.Id).Select(x => new TagDto
            {
                Id = x.Tag.Id, Name = x.Tag.Name, ColorCode = x.Tag.ColorCode
            }).ToList();
        }

        return tasks;
    }

    public async Task<TaskDetailDto?> GetTaskDetailAsync(Guid taskId, Guid requestingUserId)
    {
        var task = await (from t in _db.Tasks.AsNoTracking()
            join p in _db.Projects.AsNoTracking() on t.ProjectId equals p.Id
            where t.Id == taskId
            select new TaskDetailDto
            {
                Id = t.Id, ProjectId = p.Id, ProjectName = p.Name,
                Title = t.Title, Description = t.Description, Status = t.Status,
                Priority = t.Priority, Deadline = t.Deadline, IsPrivate = t.IsPrivate,
                IsPinned = t.IsPinned, ContributesToProgress = t.ContributesToProgress,
                AssigneeId = t.AssigneeId, CreatorId = t.CreatorId, CreatedAt = t.CreatedAt
            }).FirstOrDefaultAsync();
        if (task == null) return null;

        // Private task masking
        if (task.IsPrivate)
        {
            var isManager = await _db.ProjectMembers.AnyAsync(pm =>
                pm.ProjectId == task.ProjectId && pm.UserId == requestingUserId && pm.Role == "Manager");
            if (!isManager && task.CreatorId != requestingUserId && task.AssigneeId != requestingUserId)
            {
                task.Title = $"Restricted Task #{task.Id.ToString()[..8]}";
                task.Description = null;
            }
        }

        if (task.AssigneeId.HasValue)
        {
            var assignee = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == task.AssigneeId.Value);
            if (assignee != null) { task.AssigneeName = assignee.FullName; task.AssigneeAvatar = assignee.AvatarUrl; }
        }
        task.CommentCount = await _db.Comments.CountAsync(c => c.TaskId == taskId && !c.IsDeleted);

        var tags = await _db.TaskTags.AsNoTracking()
            .Where(tt => tt.TaskId == taskId)
            .Join(_db.Tags.AsNoTracking(), tt => tt.TagId, t => t.Id, (tt, t) => t)
            .ToListAsync();
        task.Tags = tags.Select(t => new TagDto { Id = t.Id, Name = t.Name, ColorCode = t.ColorCode }).ToList();

        return task;
    }

    public async Task<TaskEntity?> GetByIdAsync(Guid taskId) =>
        await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);

    public async Task<TaskEntity> CreateAsync(CreateTaskRequest request, Guid creatorId)
    {
        var task = new TaskEntity
        {
            Id = Guid.NewGuid(), ProjectId = request.ProjectId, Title = request.Title.Trim(),
            Description = request.Description, CreatorId = creatorId, AssigneeId = request.AssigneeId,
            Deadline = request.Deadline.HasValue ? DateTime.SpecifyKind(request.Deadline.Value, DateTimeKind.Utc) : null,
            Status = TaskStatusEnum.Todo, Priority = request.Priority, IsPrivate = request.IsPrivate,
            ContributesToProgress = request.ContributesToProgress, CreatedAt = DateTime.UtcNow
        };
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        return task;
    }

    public async Task<bool> UpdateAsync(UpdateTaskRequest request, Guid userId)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == request.TaskId);
        if (task == null) return false;
        if (!await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == userId && pm.Role == "Manager"))
            return false;
        task.Title = request.Title.Trim(); task.Description = request.Description;
        task.AssigneeId = request.AssigneeId; task.Status = request.Status;
        task.Priority = request.Priority; task.IsPrivate = request.IsPrivate;
        task.ContributesToProgress = request.ContributesToProgress; task.UpdatedAt = DateTime.UtcNow;
        task.Deadline = request.Deadline.HasValue ? DateTime.SpecifyKind(request.Deadline.Value, DateTimeKind.Utc) : null;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid taskId, Guid userId)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null) return false;
        if (!await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == userId && pm.Role == "Manager"))
            return false;
        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<TaskStatusChangeResult> ChangeStatusAsync(Guid taskId, TaskStatusEnum newStatus, Guid userId)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null) return new() { Success = false, Error = "Task not found" };
        if (!Enum.IsDefined(typeof(TaskStatusEnum), newStatus))
            return new() { Success = false, Error = "Invalid status" };

        var isManager = await _db.ProjectMembers.AnyAsync(pm =>
            pm.ProjectId == task.ProjectId && pm.UserId == userId && pm.Role == "Manager");
        if (!isManager && !IsAllowedMemberTransition(task.Status, newStatus))
            return new() { Success = false, Error = $"Members cannot change from {task.Status} to {newStatus}" };

        task.Status = newStatus; task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new() { Success = true, NewStatus = newStatus };
    }

    public async Task<TaskStatusChangeResult> CycleStatusAsync(Guid taskId, Guid userId)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null) return new() { Success = false, Error = "Task not found" };
        return await ChangeStatusAsync(taskId, GetNextStatus(task.Status), userId);
    }

    public async Task<TaskTimelineData> GetTimelineAsync(Guid projectId, string range = "30d")
    {
        int days = range switch { "14d" => 14, "90d" => 90, _ => 30 };
        var from = DateTime.UtcNow.Date.AddDays(-(days - 1));
        var tasks = await _db.Tasks.Where(t => t.ProjectId == projectId &&
            (t.CreatedAt >= from || (t.Status == TaskStatusEnum.Completed && t.UpdatedAt.HasValue && t.UpdatedAt.Value >= from)))
            .Select(t => new { t.CreatedAt, t.UpdatedAt, t.Status }).ToListAsync();
        var labels = Enumerable.Range(0, days).Select(i => from.AddDays(i).ToString("yyyy-MM-dd")).ToList();
        var created = labels.ToDictionary(x => x, _ => 0);
        var completed = labels.ToDictionary(x => x, _ => 0);
        foreach (var t in tasks)
        {
            var cKey = t.CreatedAt.ToUniversalTime().Date.ToString("yyyy-MM-dd");
            if (created.ContainsKey(cKey)) created[cKey]++;
            if (t.Status == TaskStatusEnum.Completed && t.UpdatedAt.HasValue)
            {
                var dKey = t.UpdatedAt.Value.ToUniversalTime().Date.ToString("yyyy-MM-dd");
                if (completed.ContainsKey(dKey)) completed[dKey]++;
            }
        }
        return new TaskTimelineData
        {
            Labels = labels,
            Created = labels.Select(l => created[l]).ToArray(),
            Completed = labels.Select(l => completed[l]).ToArray()
        };
    }

    // ── TAGS ───────────────────────────────────────────
    public async Task<bool> AddTagAsync(Guid taskId, Guid tagId, Guid userId)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null) return false;

        var isManager = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == userId && pm.Role == "Manager");
        if (!isManager && task.CreatorId != userId && task.AssigneeId != userId) return false; // Basic permissions

        var tagExists = await _db.Tags.AnyAsync(t => t.Id == tagId && t.ProjectId == task.ProjectId);
        if (!tagExists) return false;

        var alreadyHasTag = await _db.TaskTags.AnyAsync(tt => tt.TaskId == taskId && tt.TagId == tagId);
        if (alreadyHasTag) return true;

        _db.TaskTags.Add(new TaskTagEntity { TaskId = taskId, TagId = tagId });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveTagAsync(Guid taskId, Guid tagId, Guid userId)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null) return false;

        var isManager = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == userId && pm.Role == "Manager");
        if (!isManager && task.CreatorId != userId && task.AssigneeId != userId) return false;

        var tt = await _db.TaskTags.FirstOrDefaultAsync(x => x.TaskId == taskId && x.TagId == tagId);
        if (tt == null) return false;

        _db.TaskTags.Remove(tt);
        await _db.SaveChangesAsync();
        return true;
    }

        public async Task<bool> SyncTagsAsync(Guid taskId, List<Guid> tagIds, Guid userId)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null) return false;

        var isManager = await _db.ProjectMembers.AnyAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == userId && pm.Role == "Manager");
        if (!isManager && task.CreatorId != userId && task.AssigneeId != userId) return false;

        var existingTags = await _db.TaskTags.Where(tt => tt.TaskId == taskId).ToListAsync();
        
        var tagsToRemove = existingTags.Where(tt => !tagIds.Contains(tt.TagId)).ToList();
        _db.TaskTags.RemoveRange(tagsToRemove);

        var existingTagIds = existingTags.Select(tt => tt.TagId).ToList();
        var tagsToAdd = tagIds.Where(id => !existingTagIds.Contains(id)).Select(id => new TaskTagEntity { TaskId = taskId, TagId = id }).ToList();
        _db.TaskTags.AddRange(tagsToAdd);

        await _db.SaveChangesAsync();
        return true;
    }

    private static bool IsAllowedMemberTransition(TaskStatusEnum current, TaskStatusEnum target) =>
        (current, target) switch
        {
            (TaskStatusEnum.Todo, TaskStatusEnum.InProgress) => true,
            (TaskStatusEnum.InProgress, TaskStatusEnum.Review) => true,
            (TaskStatusEnum.InProgress, TaskStatusEnum.OnHold) => true,
            (TaskStatusEnum.OnHold, TaskStatusEnum.InProgress) => true,
            _ => false
        };

    private static TaskStatusEnum GetNextStatus(TaskStatusEnum current) =>
        current switch
        {
            TaskStatusEnum.Todo => TaskStatusEnum.InProgress,
            TaskStatusEnum.InProgress => TaskStatusEnum.Review,
            TaskStatusEnum.Review => TaskStatusEnum.Completed,
            TaskStatusEnum.OnHold => TaskStatusEnum.InProgress,
            TaskStatusEnum.Completed => TaskStatusEnum.Todo,
            TaskStatusEnum.Cancelled => TaskStatusEnum.Todo,
            _ => TaskStatusEnum.Todo
        };
}


