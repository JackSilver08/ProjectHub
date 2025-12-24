using System.ComponentModel.DataAnnotations;

namespace PROJECTHUB_ENTERPRISE.Dtos
{
    public class WikiEditDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;
    }
}
