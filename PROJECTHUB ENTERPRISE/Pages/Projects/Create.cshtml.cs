using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PROJECTHUB_ENTERPRISE.Data;
using PROJECTHUB_ENTERPRISE.Models;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace PROJECTHUB_ENTERPRISE.Pages.Projects
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _db;

        public CreateModel(AppDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public ProjectEntity Project { get; set; } = new();

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized();

            var userId = Guid.Parse(userIdClaim);

            // ====== SET DATA BẮT BUỘC ======
            Project.Id = Guid.NewGuid();
            Project.ManagerId = userId;
            Project.CreatedAt = DateTime.UtcNow;
            Project.IsArchived = false;

            // ⭐ BẮT BUỘC: SLUG (FIX LỖI DB)
            Project.Slug = GenerateSlug(Project.Name);

            // 1️⃣ LƯU PROJECT
            _db.Projects.Add(Project);
            await _db.SaveChangesAsync();

            // 2️⃣ LƯU PROJECT_MEMBER
            var member = new ProjectMemberEntity
            {
                ProjectId = Project.Id,
                UserId = userId,
                Role = "Manager",
                JoinedAt = DateTime.UtcNow
            };

            _db.ProjectMembers.Add(member);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Index");
        }

        // ====== HELPER: TẠO SLUG ======
        private string GenerateSlug(string input)
        {
            var slug = input.ToLowerInvariant();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
            return slug;
        }
    }
}
