using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;
using PROJECTHUB_ENTERPRISE.Models;
using PROJECTHUB_ENTERPRISE.Services.Interfaces;

namespace PROJECTHUB_ENTERPRISE.Services.Implementations;

public class WikiService : IWikiService
{
    private readonly AppDbContext _db;
    public WikiService(AppDbContext db) { _db = db; }

    public async Task<List<WikiPageDto>> GetByProjectAsync(Guid projectId)
    {
        return await (from w in _db.WikiPages.AsNoTracking()
            where w.ProjectId == projectId
            orderby w.Title
            select new WikiPageDto
            {
                Id = w.Id, ProjectId = w.ProjectId, Title = w.Title,
                Slug = w.Slug, Content = w.Content,
                LastUpdatedBy = w.LastUpdatedBy, UpdatedAt = w.UpdatedAt
            }).ToListAsync();
    }

    public async Task<WikiPageDto?> GetByIdAsync(Guid wikiId)
    {
        var w = await _db.WikiPages.FindAsync(wikiId);
        if (w == null) return null;
        return new WikiPageDto
        {
            Id = w.Id, ProjectId = w.ProjectId, Title = w.Title,
            Slug = w.Slug, Content = w.Content,
            LastUpdatedBy = w.LastUpdatedBy, UpdatedAt = w.UpdatedAt
        };
    }

    public async Task<WikiPage> CreateAsync(Guid projectId, string title, string content, Guid userId)
    {
        var wiki = new WikiPage
        {
            Id = Guid.NewGuid(), ProjectId = projectId,
            Title = title, Content = content,
            UpdatedAt = DateTime.UtcNow, LastUpdatedBy = userId
        };
        _db.WikiPages.Add(wiki);
        await _db.SaveChangesAsync();
        return wiki;
    }

    public async Task<bool> UpdateAsync(Guid wikiId, string title, string content, Guid userId)
    {
        var wiki = await _db.WikiPages.FindAsync(wikiId);
        if (wiki == null) return false;
        wiki.Title = title; wiki.Content = content;
        wiki.UpdatedAt = DateTime.UtcNow; wiki.LastUpdatedBy = userId;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid wikiId, Guid userId)
    {
        var wiki = await _db.WikiPages.FindAsync(wikiId);
        if (wiki == null) return false;
        _db.WikiPages.Remove(wiki);
        await _db.SaveChangesAsync();
        return true;
    }
}
