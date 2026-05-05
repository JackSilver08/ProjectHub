using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;
using PROJECTHUB_ENTERPRISE.Models;
using PROJECTHUB_ENTERPRISE.Services.Interfaces;
using System.Text.RegularExpressions;

namespace PROJECTHUB_ENTERPRISE.Services.Implementations;

public class CommentService : ICommentService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;

    public CommentService(AppDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<List<CommentItemDto>> GetCommentTreeAsync(Guid taskId, Guid? currentUserId = null)
    {
        var comments = await _db.Comments
            .Where(c => c.TaskId == taskId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentItemDto
            {
                Id = c.Id, TaskId = c.TaskId, ParentId = c.ParentId,
                UserId = c.UserId, Content = c.Content, CreatedAt = c.CreatedAt,
                Attachments = c.Attachments.Select(a => new AttachmentDto
                {
                    FileName = a.FileName, FilePath = a.FilePath,
                    ContentType = a.ContentType, FileSize = a.FileSize
                }).ToList(),
                Replies = new List<CommentItemDto>()
            }).ToListAsync();

        var commentIds = comments.Select(c => c.Id).ToList();
        var votes = await _db.CommentVotes
            .Where(v => commentIds.Contains(v.CommentId))
            .ToListAsync();

        foreach (var c in comments)
        {
            var cVotes = votes.Where(v => v.CommentId == c.Id).ToList();
            c.Upvotes = cVotes.Count(v => v.IsUpvote);
            c.Downvotes = cVotes.Count(v => !v.IsUpvote);
            if (currentUserId.HasValue)
            {
                var myVote = cVotes.FirstOrDefault(v => v.UserId == currentUserId.Value);
                if (myVote != null) c.CurrentUserVote = myVote.IsUpvote;
            }
        }

        // Map user info
        var userIds = comments.Select(c => c.UserId).Distinct().ToList();
        var users = await _db.Users.Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        foreach (var c in comments)
        {
            if (users.TryGetValue(c.UserId, out var u))
            {
                c.UserName = u.FullName;
                c.AvatarUrl = u.AvatarUrl;
            }
        }

        // Build tree
        var lookup = comments.ToDictionary(c => c.Id);
        var roots = new List<CommentItemDto>();
        foreach (var c in comments)
        {
            if (c.ParentId == null) roots.Add(c);
            else if (lookup.TryGetValue(c.ParentId.Value, out var parent))
                parent.Replies.Add(c);
        }
        return roots;
    }

    public async Task<CommentEntity> CreateAsync(CreateCommentRequest request, Guid userId)
    {
        var comment = new CommentEntity
        {
            TaskId = request.TaskId, UserId = userId,
            Content = request.Content, ParentId = request.ParentId,
            CreatedAt = DateTime.UtcNow, IsDeleted = false
        };
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        // Save attachments
        if (request.Attachments != null && request.Attachments.Any())
        {
            var uploadRoot = Path.Combine("wwwroot", "uploads", "comments", comment.Id.ToString());
            Directory.CreateDirectory(uploadRoot);
            foreach (var file in request.Attachments)
            {
                if (file.Length == 0) continue;
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadRoot, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
                _db.CommentAttachments.Add(new CommentAttachment
                {
                    Id = Guid.NewGuid(), CommentId = comment.Id,
                    FileName = file.FileName,
                    FilePath = $"/uploads/comments/{comment.Id}/{fileName}",
                    ContentType = file.ContentType, FileSize = file.Length,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
        }

        // Process @mentions
        await _notificationService.CreateMentionNotificationsAsync(
            request.TaskId, comment.Id, request.Content, userId);

        return comment;
    }

    public async Task<bool> DeleteAsync(long commentId, Guid userId)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentId);
        if (comment == null) return false;
        if (comment.UserId != userId) return false;
        comment.IsDeleted = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<VoteResultDto> VoteAsync(long commentId, Guid userId, bool isUpvote)
    {
        var existingVote = await _db.CommentVotes
            .FirstOrDefaultAsync(v => v.CommentId == commentId && v.UserId == userId);

        if (existingVote != null)
        {
            if (existingVote.IsUpvote == isUpvote)
            {
                // Unvote if toggling the same
                _db.CommentVotes.Remove(existingVote);
            }
            else
            {
                // Switch vote
                existingVote.IsUpvote = isUpvote;
            }
        }
        else
        {
            // New vote
            _db.CommentVotes.Add(new VoteEntity
            {
                CommentId = commentId,
                UserId = userId,
                IsUpvote = isUpvote,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        var upvotes = await _db.CommentVotes.CountAsync(v => v.CommentId == commentId && v.IsUpvote);
        var downvotes = await _db.CommentVotes.CountAsync(v => v.CommentId == commentId && !v.IsUpvote);

        return new VoteResultDto { Upvotes = upvotes, Downvotes = downvotes };
    }

    public List<Guid> ExtractMentionedUserIds(string content)
    {
        var result = new List<Guid>();
        var matches = Regex.Matches(content, @"@\{([0-9a-fA-F-]{36})\}\|");
        foreach (Match m in matches)
            if (Guid.TryParse(m.Groups[1].Value, out var id))
                result.Add(id);
        return result.Distinct().ToList();
    }
}
