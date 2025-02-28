namespace TwoFactorAuthProj.interfaces;

public interface IOtpGoogleAuth
{
    (string qrCodeUrl, string manualEntry) GenerateOtp<T>(string username, T userId);

    bool VerifyGoogleOtp(string otp);
}
