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
        public DbSet<WikiPage> WikiPages => Set<WikiPage>();
        public DbSet<CommentEntity> Comments { get; set; }

        public DbSet<TaskEntity> Tasks { get; set; }
        public DbSet<NotificationEntity> Notifications { get; set; }
        public DbSet<CommentAttachment> CommentAttachments { get; set; }


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

            // ================= COMMENTS =================
            builder.Entity<CommentEntity>(e =>
            {
                e.ToTable("comments");
                e.HasKey(c => c.Id);

                e.Property(c => c.Id).HasColumnName("id");
                e.Property(c => c.TaskId).HasColumnName("task_id");
                e.Property(c => c.UserId).HasColumnName("user_id");
                e.Property(c => c.Content).HasColumnName("content");
                e.Property(c => c.ParentId).HasColumnName("parent_id");
                e.Property(c => c.CreatedAt).HasColumnName("created_at");
                e.Property(c => c.IsDeleted).HasColumnName("is_deleted");

                // 🔗 USER
                e.HasOne(c => c.User)
                 .WithMany()
                 .HasForeignKey(c => c.UserId);

                // 🔗 TASK
                e.HasOne(c => c.Task)
                 .WithMany()
                 .HasForeignKey(c => c.TaskId);

                // 🌳 SELF-REFERENCE (REPLY)
                e.HasOne(c => c.Parent)
                 .WithMany(p => p.Replies)
                 .HasForeignKey(c => c.ParentId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<NotificationEntity>(e =>
            {
                e.ToTable("notifications");
                e.HasKey(n => n.Id);

                e.Property(n => n.Id).HasColumnName("id");
                e.Property(n => n.UserId).HasColumnName("user_id"); // BIGINT
                e.Property(n => n.Type).HasColumnName("type");
                e.Property(n => n.Message).HasColumnName("message");
                e.Property(n => n.TaskId).HasColumnName("task_id");
                e.Property(n => n.CommentId).HasColumnName("comment_id");
                e.Property(n => n.IsRead).HasColumnName("is_read");
                e.Property(n => n.CreatedAt).HasColumnName("created_at");
                e.Property(n => n.LinkUrl).HasColumnName("link_url");
            });

            builder.Entity<CommentAttachment>(e =>
            {
                e.ToTable("comment_attachments");
                e.HasKey(x => x.Id);

                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.CommentId).HasColumnName("comment_id");
                e.Property(x => x.FileName).HasColumnName("file_name");
                e.Property(x => x.FilePath).HasColumnName("file_path");
                e.Property(x => x.ContentType).HasColumnName("content_type");
                e.Property(x => x.FileSize).HasColumnName("file_size");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");

                e.HasOne(x => x.Comment)
                 .WithMany(c => c.Attachments)
                 .HasForeignKey(x => x.CommentId)
                 .OnDelete(DeleteBehavior.Cascade);
            });


        }
    }
}
