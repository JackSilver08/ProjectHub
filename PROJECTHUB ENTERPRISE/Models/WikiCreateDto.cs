using System.ComponentModel.DataAnnotations;

namespace PROJECTHUB_ENTERPRISE.Dtos
{
    public class WikiCreateDto
    {
        [Required]
        public Guid ProjectId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;
    }
}
