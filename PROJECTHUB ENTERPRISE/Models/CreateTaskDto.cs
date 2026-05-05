using System;

namespace PROJECTHUB_ENTERPRISE.Dtos
{
    public class CreateTaskDto
    {
        public Guid ProjectId { get; set; }
        public string Title { get; set; }
    public string? Description { get; set; }
    public List<Guid>? TagIds { get; set; } = new();
        public Guid? AssigneeId { get; set; }

        // ? THÊM DÒNG NÀY
        public DateTime? Deadline { get; set; }
    }
}


