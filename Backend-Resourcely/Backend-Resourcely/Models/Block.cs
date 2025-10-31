using System;
using System.Collections.Generic;

namespace Backend_Resourcely.Models
{
    public class Block
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int FloorId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Floor Floor { get; set; } = null!;
        public virtual ICollection<Resource> Resources { get; set; } = new List<Resource>();
    }
}