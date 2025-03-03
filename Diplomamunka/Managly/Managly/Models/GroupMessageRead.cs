using Managly.Models;

public class GroupMessageRead
{
    public int MessageId { get; set; }
    public string UserId { get; set; }
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;

    public virtual GroupMessage Message { get; set; }
    public virtual User User { get; set; }
} 