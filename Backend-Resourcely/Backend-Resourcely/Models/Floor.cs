using System;
using System.Collections.Generic;

namespace Backend_Resourcely.Models
{
    public class Floor
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int BuildingId { get; set; }
        public int FloorNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Building Building { get; set; } = null!;
        public virtual ICollection<Block> Blocks { get; set; } = new List<Block>();
    }
}