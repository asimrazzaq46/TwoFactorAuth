namespace TwoFactorAuthProj.Entities;

public class OtpFailedAttempt
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int FailedAttemptsCount { get; set; }
    public DateTime? LockoutExpiration { get; set; }
}
