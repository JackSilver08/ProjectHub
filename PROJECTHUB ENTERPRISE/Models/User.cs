using System;

namespace PROJECTHUB_ENTERPRISE.Models
{
    public class User
    {
        public Guid Id { get; set; }

        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
