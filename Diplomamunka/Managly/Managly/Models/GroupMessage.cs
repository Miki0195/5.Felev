using Managly.Models;

public class GroupMessage
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string SenderId { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public virtual GroupChat Group { get; set; }
    public virtual User Sender { get; set; }
    public virtual ICollection<GroupMessageRead> ReadBy { get; set; } = new List<GroupMessageRead>();
} 