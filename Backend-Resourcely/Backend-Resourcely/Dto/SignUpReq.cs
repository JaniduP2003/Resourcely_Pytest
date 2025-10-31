using System;

namespace Backend_Resourcely.Dto;

public class SignUpReq
{

    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Username { get; set; }
    
}
