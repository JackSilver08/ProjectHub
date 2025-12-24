using System.ComponentModel.DataAnnotations;

namespace PROJECTHUB_ENTERPRISE.Models
{
    public class CreateProjectVM
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
