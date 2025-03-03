using System;
using System.Collections.Generic;
using Managly.Models;

public class GroupChat
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedById { get; set; }
    public virtual User CreatedBy { get; set; }
    public virtual ICollection<GroupMember> Members { get; set; }
    public virtual ICollection<GroupMessage> Messages { get; set; }
} 