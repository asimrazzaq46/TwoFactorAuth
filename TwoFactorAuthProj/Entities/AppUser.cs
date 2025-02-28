using Microsoft.AspNetCore.Identity;
using TwoFactorAuthProj.Enums;

namespace TwoFactorAuthProj.Entities;

public class AppUser : IdentityUser
{
    public required string Name { get; set; }
    public AuthType? AuthType { get; set; }
    public bool? IsPresistent { get; set; }
    public string? VerificationCode { get; set; }
    public DateTime? VerificationCodeExpiry {get; set; }

    }
