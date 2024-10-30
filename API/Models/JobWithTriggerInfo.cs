using System;

namespace API.Models
{
    public class JobWithTriggerInfo
    {
        public string JobKey { get; set; }
        public string Group { get; set; }
        public string Description { get; set; }
        public string TriggerKey { get; set; }
        public string TriggerGroup { get; set; }
        public DateTime? NextFireTime { get; set; }
        public DateTime? PreviousFireTime { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime? EndAt { get; set; }
        public string TriggerState { get; set; }
        public TimeSpan? RepeatInterval { get; set; }
        public string CronExpression { get; set; }
    }
}