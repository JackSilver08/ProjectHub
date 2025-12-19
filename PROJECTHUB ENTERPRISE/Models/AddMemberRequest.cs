namespace PROJECTHUB_ENTERPRISE.Models
{
    public class AddMemberRequest
    {
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
    }
}
