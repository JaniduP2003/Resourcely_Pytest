using System;

namespace Backend_Resourcely.Models
{
    public class Booking
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; } = null!; 
        public int ResourceId { get; set; }
        public DateTime BookingAt { get; set; }
        public DateTime EndAt { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Contact { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Resource Resource { get; set; } = null!;
    }
}
