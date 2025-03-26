using Managly.Models.Enums;
namespace Managly.Models.DTOs.Notification
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string Link { get; set; }
        public DateTime Timestamp { get; set; }
        public NotificationType Type { get; set; }
        public string RelatedUserId { get; set; }
        public Dictionary<string, string> MetaData { get; set; }
    }

    public class NotificationGroupDto
    {
        public NotificationType Type { get; set; }
        public string GroupTitle { get; set; }
        public List<NotificationSenderGroupDto> SenderGroups { get; set; }
        public int UnreadCount { get; set; }
    }

    public class CreateNotificationDto
    {
        public string UserId { get; set; }
        public string Message { get; set; }
        public string Link { get; set; }
        public NotificationType Type { get; set; }
        public Dictionary<string, string> MetaData { get; set; }
        public int? ProjectId { get; set; }
        public int? TaskId { get; set; }
        public string RelatedUserId { get; set; }
    }

    public class NotificationSenderGroupDto
    {
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public List<NotificationDto> Notifications { get; set; }
        public int UnreadCount { get; set; }
    }
} 