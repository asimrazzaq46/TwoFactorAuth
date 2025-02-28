using Google.Authenticator;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using TwoFactorAuthProj.interfaces;

namespace TwoFactorAuthProj.Services;

public class OtpGoogleAuth(IConfiguration _config) : IOtpGoogleAuth
{

    private readonly string secret = _config["otp:secret"] ??
        throw new Exception("secret is not provided");

    private readonly string Seconds = _config["otp:StepSeconds"] ?? string.Empty;
  




    public (string qrCodeUrl,string manualEntry) GenerateOtp<T>(string appName,T userId)
    {
        if(userId == null) throw new ArgumentNullException("user id cannot be null");

        var userIdString = userId.ToString();

        var tfa = new TwoFactorAuthenticator();
        var setupCode = tfa.GenerateSetupCode(appName ,userIdString , secret , false , 3);

        string qrImageCode = setupCode.QrCodeSetupImageUrl;
        string manualEntrySetup = setupCode.ManualEntryKey;

        return (qrImageCode,manualEntrySetup);

    }


    public bool VerifyGoogleOtp(string otp)
    {
        var tfa = new TwoFactorAuthenticator();

        if(!string.IsNullOrEmpty(Seconds) && int.TryParse(Seconds, out int parsedSeconds) && parsedSeconds > 0)
        {
          return  tfa.ValidateTwoFactorPIN(secret, otp, timeTolerance: TimeSpan.FromSeconds(parsedSeconds));
        }

        return tfa.ValidateTwoFactorPIN(secret,otp,timeTolerance:TimeSpan.Zero);
    }


}
