using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Backend_Resourcely.Models
{
    public class Batch
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<StudentProfile> Students { get; set; } = new List<StudentProfile>();
    }
}
