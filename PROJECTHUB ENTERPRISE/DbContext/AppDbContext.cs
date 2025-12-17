using Microsoft.EntityFrameworkCore;
using PROJECTHUB_ENTERPRISE.Models;

namespace PROJECTHUB_ENTERPRISE.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
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
        }
    }
}