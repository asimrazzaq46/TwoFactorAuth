using TwoFactorAuthProj.Enums;

namespace TwoFactorAuthProj.interfaces;

public interface IDataBaseOtpService
{
    Task<(string otp, DateTime expiry)> GenerateOtp<T>(T userid, string purpose, bool alphanumeric = false);
    Task<bool> VerifyOtpAsync<T>(T userId, string otp, string purpose);

}
