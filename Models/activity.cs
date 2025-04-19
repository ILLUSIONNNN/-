using System;

namespace shetuan.Models
{
    public class Activity
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime EventTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CreatorEmail { get; set; } = string.Empty;
    }
}