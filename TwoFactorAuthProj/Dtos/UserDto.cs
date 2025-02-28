using TwoFactorAuthProj.Enums;

namespace TwoFactorAuthProj.Dtos;

public class UserDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public  string? UserName { get; set; }
    public  string? VerificationCode { get; set; }
    public required string PhoneNumber { get; set; }
    public string? AuthType { get; set; }
    public bool? IsPresistent { get; set; }
}
