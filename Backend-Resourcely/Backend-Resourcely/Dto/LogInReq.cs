using System;

namespace Backend_Resourcely.Dto;

public class LogInReq
{

    public required string Email { get; set; }
    public required string Password { get; set; }
    
}
