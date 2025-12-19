using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Models;

namespace PROJECTHUB_ENTERPRISE.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
        public DbSet<ProjectMemberEntity> ProjectMembers => Set<ProjectMemberEntity>();

        public DbSet<TaskEntity> Tasks { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ================= USERS =================
            builder.Entity<User>(e =>
            {
                e.ToTable("users");
                e.HasKey(x => x.Id);

                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.Username).HasColumnName("username");
                e.Property(x => x.Email).HasColumnName("email");
                e.Property(x => x.PasswordHash).HasColumnName("password_hash");
                e.Property(x => x.FullName).HasColumnName("full_name");
                e.Property(x => x.AvatarUrl).HasColumnName("avatar_url");
                e.Property(x => x.JobTitle).HasColumnName("job_title");
                e.Property(x => x.Department).HasColumnName("department");
                e.Property(x => x.IsActive).HasColumnName("is_active");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
            });

            // ================= PROJECTS =================
            builder.Entity<ProjectEntity>(e =>
            {
                e.ToTable("projects");
                e.HasKey(p => p.Id);

                e.Property(p => p.Id).HasColumnName("id");
                e.Property(p => p.Name).HasColumnName("name");
                e.Property(p => p.Description).HasColumnName("description");
                e.Property(p => p.ManagerId).HasColumnName("manager_id");
                e.Property(p => p.CreatedAt).HasColumnName("created_at");
                e.Property(p => p.UpdatedAt).HasColumnName("updated_at");
                e.Property(p => p.IsArchived).HasColumnName("is_archived");
            });

            // ================= PROJECT_MEMBERS =================
            builder.Entity<ProjectMemberEntity>(e =>
            {
                e.ToTable("project_members");

                // ✅ COMPOSITE PRIMARY KEY
                e.HasKey(pm => new { pm.ProjectId, pm.UserId });

                e.Property(pm => pm.ProjectId).HasColumnName("project_id");
                e.Property(pm => pm.UserId).HasColumnName("user_id");
                e.Property(pm => pm.Role).HasColumnName("role");
                e.Property(pm => pm.JoinedAt).HasColumnName("joined_at");
            });
        }
    }
}
