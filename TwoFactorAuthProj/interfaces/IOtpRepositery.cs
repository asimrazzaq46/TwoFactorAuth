using TwoFactorAuthProj.Entities;
using TwoFactorAuthProj.Enums;

namespace TwoFactorAuthProj.interfaces;

public interface IOtpRepositery
{
    Task AddOtpAsync(OtpRecord otpRecord);
    Task<OtpRecord> GetOtpAsync(string userId, string purpose);
    void RemoveOtp(OtpRecord otpRecord);
    Task RemoveOtpByUserIdAndPurpose(string userId, string purpose);
    Task<OtpFailedAttempt?> GetFailedAttemptAsync(string userId);
    Task AddFailedAttemptAsync(OtpFailedAttempt failedAttempt);
    Task ResetFailedAttemptsAsync(string userId);
    Task LockOutUserAsync(string userId, DateTime lockoutExpiration);
    //Task UpdateCounterAsync(string userId, long newCounter);
    //Task<long?> GetLastUsedCounterAsync(string userId);
    Task SavechangesAsync();
}
