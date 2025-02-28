using Twilio.Types;
using TwoFactorAuthProj.Enums;

namespace TwoFactorAuthProj.Dtos;

public class UpdateDto
{
    public  string? Name { get; set; }
    //public  string Email { get; set; }
    public string? UserName { get; set; }
    public string? VerificationCode { get; set; }
    public  string? PhoneNumber { get; set; }
    public AuthType? AuthType { get; set; }
    public bool? IsPresistent { get; set; }
}
