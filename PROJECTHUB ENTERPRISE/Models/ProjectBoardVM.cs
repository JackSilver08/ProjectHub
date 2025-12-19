namespace PROJECTHUB_ENTERPRISE.Models
{
    public class ProjectBoardVM
    {
        public List<TaskBoardItemVM> Todo { get; set; } = new();
        public List<TaskBoardItemVM> InProgress { get; set; } = new();
        public List<TaskBoardItemVM> Review { get; set; } = new();
        public List<TaskBoardItemVM> Done { get; set; } = new();
    }

}
