namespace Aimy.Core.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public string? Role { get; set; }

    public User()
    {
        Id = Guid.NewGuid();
    }
}
