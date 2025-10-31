using System;
using System.Collections.Generic;

namespace Backend_Resourcely.Models;

public class User   //single nor plural only for one user
{

    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public DateTime CreatedAt { get; set; }

     public StudentProfile? StudentProfile { get; set; }
     public virtual LecturerProfile? LecturerProfile { get; set; }
    
}
