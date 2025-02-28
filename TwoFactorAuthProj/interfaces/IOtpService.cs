namespace TwoFactorAuthProj.interfaces;

public interface IOtpService
{
    (string Otp, DateTime ExpiryTime) GenerateOtp<T>(string secret, T userId,  string purpose, bool alphanumeric = false);
    (string Otp, DateTime ExpiryTime) GenerateOtp<T>(string secret, T userId, bool alphanumeric = false);
    bool VerifyOtp<T>(string secret,string otp, T userId, bool alphanumeric = false );
    bool VerifyOtp<T>(string secret,string otp, T userId, string purpose, bool alphanumeric = false );
}
