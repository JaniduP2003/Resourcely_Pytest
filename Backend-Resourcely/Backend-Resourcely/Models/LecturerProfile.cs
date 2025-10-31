namespace Backend_Resourcely.Models
{
    public class LecturerProfile
    {
        public int UserId { get; set; }
        public int DepartmentId { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Department Department { get; set; } = null!;
    }
}
