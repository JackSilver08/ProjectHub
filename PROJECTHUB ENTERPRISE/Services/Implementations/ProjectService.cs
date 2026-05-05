using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;
using PROJECTHUB_ENTERPRISE.Services.Interfaces;
using System.Text.RegularExpressions;
using TaskStatusEnum = PROJECTHUB_ENTERPRISE.Models.TaskStatus;

namespace PROJECTHUB_ENTERPRISE.Services.Implementations;

public class ProjectService : IProjectService
{
    private readonly AppDbContext _db;

    public ProjectService(AppDbContext db)
    {
        _db = db;
    }

    // ── QUERY ──────────────────────────────────────────

    public async Task<List<ProjectSummaryDto>> GetUserProjectsAsync(
        Guid userId, string? search = null, bool? archived = null)
    {
        var query =
            from pm in _db.ProjectMembers.AsNoTracking()
            join p in _db.Projects.AsNoTracking() on pm.ProjectId equals p.Id
            where pm.UserId == userId
            select new { Project = p, pm.Role };

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Project.Name.Contains(search));

        if (archived.HasValue)
            query = query.Where(x => x.Project.IsArchived == archived.Value);

        var raw = await query.OrderBy(x => x.Project.Name).ToListAsync();

        var result = new List<ProjectSummaryDto>();
        foreach (var item in raw)
        {
            var dto = new ProjectSummaryDto
            {
                ProjectId = item.Project.Id,
                Name = item.Project.Name,
                Description = item.Project.Description,
                Role = item.Role,
                Status = item.Project.IsArchived ? "Archived" : "Active"
            };

            // Tính progress cho mỗi project
            var progress = await CalculateProgressAsync(item.Project.Id);
            dto.TotalTasks = progress.TotalTasks;
            dto.CompletedTasks = progress.CompletedTasks;
            dto.ProgressPercent = progress.ProgressPercent;

            result.Add(dto);
        }

        return result;
    }

    public async Task<List<ProjectSummaryDto>> GetArchivedProjectsAsync(
        Guid userId, string? search = null)
    {
        return await GetUserProjectsAsync(userId, search, archived: true);
    }

    public async Task<ProjectEntity?> GetByIdAsync(Guid projectId)
    {
        return await _db.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId);
    }

    public async Task<string?> GetUserRoleAsync(Guid projectId, Guid userId)
    {
        return await _db.ProjectMembers
            .Where(pm => pm.ProjectId == projectId && pm.UserId == userId)
            .Select(pm => pm.Role)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsManagerAsync(Guid projectId, Guid userId)
    {
        return await _db.ProjectMembers.AnyAsync(pm =>
            pm.ProjectId == projectId &&
            pm.UserId == userId &&
            pm.Role == "Manager");
    }

    public async Task<bool> IsOwnerAsync(Guid projectId, Guid userId)
    {
        return await _db.Projects.AnyAsync(p =>
            p.Id == projectId && p.ManagerId == userId);
    }

    // ── CRUD ───────────────────────────────────────────

    public async Task<ProjectEntity> CreateAsync(string name, string? description, Guid managerId)
    {
        var project = new ProjectEntity
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim(),
            ManagerId = managerId,
            Slug = GenerateSlug(name),
            IsArchived = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Projects.Add(project);

        // Tự động thêm người tạo làm Manager
        _db.ProjectMembers.Add(new ProjectMemberEntity
        {
            ProjectId = project.Id,
            UserId = managerId,
            Role = "Manager",
            JoinedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return project;
    }

    public async Task<bool> UpdateAsync(Guid projectId, string name, string? description, Guid userId)
    {
        if (!await IsManagerAsync(projectId, userId))
            return false;

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
        if (project == null) return false;

        project.Name = name?.Trim() ?? project.Name;
        project.Description = description?.Trim();
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ArchiveAsync(Guid projectId, Guid userId)
    {
        if (!await IsManagerAsync(projectId, userId))
            return false;

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
        if (project == null) return false;

        project.IsArchived = true;
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreAsync(Guid projectId, Guid userId)
    {
        if (!await IsManagerAsync(projectId, userId))
            return false;

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
        if (project == null) return false;

        project.IsArchived = false;
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid projectId, Guid userId)
    {
        if (!await IsManagerAsync(projectId, userId))
            return false;

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
        if (project == null) return false;

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
        return true;
    }

    // ── MEMBERS ────────────────────────────────────────

    public async Task<List<ProjectMemberInfo>> GetMembersAsync(Guid projectId)
    {
        return await (
            from pm in _db.ProjectMembers.AsNoTracking()
            join u in _db.Users.AsNoTracking() on pm.UserId equals u.Id
            where pm.ProjectId == projectId
            select new ProjectMemberInfo
            {
                UserId = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                AvatarUrl = u.AvatarUrl,
                Role = pm.Role,
                JoinedAt = pm.JoinedAt
            }
        ).ToListAsync();
    }

    public async Task<bool> AddMemberAsync(
        Guid projectId, Guid userId, Guid addedByUserId, string role = "Member")
    {
        if (!await IsManagerAsync(projectId, addedByUserId))
            return false;

        var exists = await _db.ProjectMembers.AnyAsync(pm =>
            pm.ProjectId == projectId && pm.UserId == userId);

        if (exists) return false;

        _db.ProjectMembers.Add(new ProjectMemberEntity
        {
            ProjectId = projectId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveMemberAsync(
        Guid projectId, Guid userId, Guid removedByUserId)
    {
        if (!await IsManagerAsync(projectId, removedByUserId))
            return false;

        var member = await _db.ProjectMembers.FirstOrDefaultAsync(pm =>
            pm.ProjectId == projectId && pm.UserId == userId);

        if (member == null) return false;

        _db.ProjectMembers.Remove(member);
        await _db.SaveChangesAsync();
        return true;
    }

    // ── PROGRESS ───────────────────────────────────────

    public async Task<ProjectProgressInfo> CalculateProgressAsync(Guid projectId)
    {
        var tasks = await _db.Tasks
            .Where(t => t.ProjectId == projectId)
            .Select(t => new { t.Status, t.ContributesToProgress })
            .ToListAsync();

        var contributing = tasks.Where(t => t.ContributesToProgress).ToList();

        // Loại bỏ Cancelled khỏi mẫu số
        var eligibleCount = contributing.Count(t => t.Status != TaskStatusEnum.Cancelled);
        var doneCount = contributing.Count(t => t.Status == TaskStatusEnum.Completed);

        var percent = eligibleCount > 0
            ? Math.Round((double)doneCount / eligibleCount * 100, 1)
            : 0;

        return new ProjectProgressInfo
        {
            TotalTasks = tasks.Count,
            CompletedTasks = tasks.Count(t => t.Status == TaskStatusEnum.Completed),
            OpenTasks = tasks.Count(t =>
                t.Status != TaskStatusEnum.Completed &&
                t.Status != TaskStatusEnum.Cancelled),
            CancelledTasks = tasks.Count(t => t.Status == TaskStatusEnum.Cancelled),
            ProgressPercent = percent,

            Todo = tasks.Count(t => t.Status == TaskStatusEnum.Todo),
            InProgress = tasks.Count(t => t.Status == TaskStatusEnum.InProgress),
            Review = tasks.Count(t => t.Status == TaskStatusEnum.Review),
            OnHold = tasks.Count(t => t.Status == TaskStatusEnum.OnHold)
        };
    }

    // ── TAGS ───────────────────────────────────────────

    public async Task<List<Models.TagEntity>> GetTagsAsync(Guid projectId)
    {
        return await _db.Tags
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<Models.TagEntity> CreateTagAsync(Guid projectId, string name, string colorCode)
    {
        var tag = new Models.TagEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = name.Trim(),
            ColorCode = colorCode.Trim()
        };
        _db.Tags.Add(tag);
        await _db.SaveChangesAsync();
        return tag;
    }

    public async Task<bool> DeleteTagAsync(Guid tagId, Guid userId)
    {
        var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == tagId);
        if (tag == null) return false;

        // Verify manager rights
        if (!await IsManagerAsync(tag.ProjectId, userId)) return false;

        _db.Tags.Remove(tag);
        await _db.SaveChangesAsync();
        return true;
    }

    // ── HELPERS ────────────────────────────────────────

    private string GenerateSlug(string input)
    {
        var slug = input.ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        if (string.IsNullOrEmpty(slug))
            slug = Guid.NewGuid().ToString("N")[..8];

        return slug;
    }
}

