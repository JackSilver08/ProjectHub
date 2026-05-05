using PROJECTHUB_ENTERPRISE.Models;

public class TaskBoardItemVM
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";

    public Guid? AssigneeId { get; set; }   // ?? THÊM DÒNG NÀY

    public string? AssigneeName { get; set; }
    public List<PROJECTHUB_ENTERPRISE.Services.Interfaces.TagDto> Tags { get; set; } = new();
    public PROJECTHUB_ENTERPRISE.Models.TaskStatus Status { get; set; }
    public int Priority { get; set; }
    public DateTime? Deadline { get; set; }
}


