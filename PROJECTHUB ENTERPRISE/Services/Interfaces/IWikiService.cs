using PROJECTHUB_ENTERPRISE.Models;

namespace PROJECTHUB_ENTERPRISE.Services.Interfaces;

/// <summary>
/// Service xử lý logic nghiệp vụ cho Wiki/Knowledge Base module.
/// </summary>
public interface IWikiService
{
    Task<List<WikiPageDto>> GetByProjectAsync(Guid projectId);
    Task<WikiPageDto?> GetByIdAsync(Guid wikiId);
    Task<WikiPage> CreateAsync(Guid projectId, string title, string content, Guid userId);
    Task<bool> UpdateAsync(Guid wikiId, string title, string content, Guid userId);
    Task<bool> DeleteAsync(Guid wikiId, Guid userId);
}

public class WikiPageDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Content { get; set; } = "";
    public Guid? LastUpdatedBy { get; set; }
    public string? LastUpdatedByName { get; set; }
    public DateTime UpdatedAt { get; set; }
}
