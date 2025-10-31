using System;
using System.Collections.Generic;

namespace Backend_Resourcely.Models
{
    public class Resource
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // e.g., "Meeting Room", "Lab", "Auditorium"
        public string Description { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int BlockId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsRestricted { get; set; } = false; // New field for restrictions
        public string RestrictedToRoles { get; set; } = string.Empty; // Comma-separated roles that can access this resource
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Block Block { get; set; } = null!;
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}