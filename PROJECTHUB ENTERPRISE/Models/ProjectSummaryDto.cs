namespace PROJECTHUB_ENTERPRISE.Models
{
    public class ProjectSummaryDto
    {
        public int TotalTasks { get; set; }
        public int OpenTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }

        public List<DailyTaskStat> DailyCompleted { get; set; }
        public List<MemberActivityDto> TopMembers { get; set; }
    }

    public class DailyTaskStat
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class MemberActivityDto
    {
        public string FullName { get; set; }
        public int Actions { get; set; }
        public DateTime LastActive { get; set; }
    }

}
