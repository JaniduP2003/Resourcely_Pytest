namespace Backend_Resourcely.Models;

public class StudentProfile
{
   
    public int UserId { get; set; }
    public User User { get; set; } = default!;

   
    public int BatchId { get; set; }
    public Batch Batch { get; set; } = default!;
}
