using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROJECTHUB_ENTERPRISE.Models
{
    [Table("wiki_pages")]
    public class WikiPage
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("project_id")]
        public Guid ProjectId { get; set; }

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("slug")]
        public string Slug { get; set; } = string.Empty;

        [Column("content")]
        public string Content { get; set; } = string.Empty;

        [Column("last_updated_by")]
        public Guid? LastUpdatedBy { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
