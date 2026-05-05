namespace PROJECTHUB_ENTERPRISE.Models
{
    /// <summary>
    /// Workflow trạng thái công việc theo đặc tả.
    /// Quy tắc:
    ///   - Member: chỉ được chuyển New → InProgress → Review
    ///   - Manager: chuyển sang bất kỳ trạng thái nào
    /// </summary>
    public enum TaskStatus
    {
        /// <summary>Mới khởi tạo, chưa ai làm.</summary>
        Todo = 0,

        /// <summary>Assignee đã nhận việc và đang thực hiện.</summary>
        InProgress = 1,

        /// <summary>Đã làm xong, chờ Manager hoặc Tester kiểm tra.</summary>
        Review = 2,

        /// <summary>Đã hoàn thành và được chấp nhận.</summary>
        Completed = 3,

        /// <summary>Tạm dừng do thiếu tài nguyên hoặc bị Block.</summary>
        OnHold = 4,

        /// <summary>Hủy bỏ, không làm nữa.</summary>
        Cancelled = 5
    }
}
