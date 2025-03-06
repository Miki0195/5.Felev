using Managly.Models;

public class GroupMember
{
    public int GroupId { get; set; }
    public string UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsAdmin { get; set; } = false;
    public virtual GroupChat Group { get; set; }
    public virtual User User { get; set; }
} 