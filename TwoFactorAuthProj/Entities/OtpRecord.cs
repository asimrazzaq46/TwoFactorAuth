using TwoFactorAuthProj.Enums;

namespace TwoFactorAuthProj.Entities;

public class OtpRecord
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Otp { get; set; }
    public DateTime Expiry { get; set; }
    public required string Purpose { get; set; }
    public long Counter { get; set; }
}
