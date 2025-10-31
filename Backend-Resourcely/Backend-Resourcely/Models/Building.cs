using System;
using System.Collections.Generic;

namespace Backend_Resourcely.Models
{
    public class Building
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<Floor> Floors { get; set; } = new List<Floor>();
    }
}