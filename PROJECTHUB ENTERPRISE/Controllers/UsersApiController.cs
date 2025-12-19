using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Data;

namespace PROJECTHUB_ENTERPRISE.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersApiController : ControllerBase
    {
        private readonly AppDbContext _db;

        public UsersApiController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Ok(new List<object>());

            var users = await _db.Users
                .Where(u => u.Email.Contains(email))
                .Select(u => new
                {
                    id = u.Id,
                    fullName = u.FullName,
                    email = u.Email
                })
                .Take(5)
                .ToListAsync();

            return Ok(users);
        }
    }
}
