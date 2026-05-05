using PROJECTHUB_ENTERPRISE.Models;

namespace PROJECTHUB_ENTERPRISE.Services.Interfaces;

/// <summary>
/// Service xử lý logic nghiệp vụ cho Comment module.
/// Bao gồm CRUD comments, attachments, và @mention extraction.
/// </summary>
public interface ICommentService
{
    /// <summary>
    /// Lấy cây bình luận đa cấp (nested tree) cho một Task.
    /// </summary>
    Task<List<CommentItemDto>> GetCommentTreeAsync(Guid taskId, Guid? currentUserId = null);

    /// <summary>
    /// Tạo comment mới (hỗ trợ nested reply qua parentId).
    /// Tự động xử lý @mention notifications.
    /// </summary>
    Task<CommentEntity> CreateAsync(CreateCommentRequest request, Guid userId);

    /// <summary>
    /// Soft-delete một comment (set IsDeleted = true).
    /// </summary>
    Task<bool> DeleteAsync(long commentId, Guid userId);

    /// <summary>
    /// Thực hiện Upvote hoặc Downvote cho một Comment.
    /// Nếu toggle = true và đã vote giống như vậy thì sẽ xóa vote (unvote).
    /// </summary>
    Task<VoteResultDto> VoteAsync(long commentId, Guid userId, bool isUpvote);

    /// <summary>
    /// Trích xuất danh sách userId được @mention trong nội dung.
    /// Format: @{guid}|Full Name
    /// </summary>
    List<Guid> ExtractMentionedUserIds(string content);
}

public class VoteResultDto
{
    public int Upvotes { get; set; }
    public int Downvotes { get; set; }
}

public class CreateCommentRequest
{
    public Guid TaskId { get; set; }
    public string Content { get; set; } = "";
    public long? ParentId { get; set; }
    public List<Microsoft.AspNetCore.Http.IFormFile>? Attachments { get; set; }
}
