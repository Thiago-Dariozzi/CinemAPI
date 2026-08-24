using System;

namespace Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = "Client";
    public bool IsActive { get; set; } = true;

}