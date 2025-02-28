namespace TwoFactorAuthProj.Dtos;

public class Registerdto
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public string? Username { get; set; }
    public required string PhoneNumber { get; set; }

}
