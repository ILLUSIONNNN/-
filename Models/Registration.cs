using System;

namespace shetuan.Models
{
    public class Registration
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public string ActivityName { get; set; } = string.Empty; // 活动名称
        public string UserEmail { get; set; } = string.Empty;    // 用户邮箱
        public string Status { get; set; } = "pending";          // 状态：pending, approved, rejected等
        public DateTime AppliedAt { get; set; }                  // 报名时间
    }
}