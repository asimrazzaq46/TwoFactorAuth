using System.Security.Cryptography;
using System.Text;
using OtpNet;
using TwoFactorAuthProj.interfaces;

namespace TwoFactorAuthProj.Services;
public class OtpService : IOtpService
{

    private const int DefaultStepsSeconds = 30; // expiry time in seconds
    private int stepSeconds;



    private const int DefaultOtpSize =6; // create 6-digit code
    private int otpSize;

    public int StepSeconds
    {
        get => stepSeconds != 0 ? stepSeconds : DefaultStepsSeconds;
        set => stepSeconds = value >= DefaultStepsSeconds ? value : DefaultStepsSeconds;
    } 

    public int OtpSize { 
        get => otpSize != 0 ? otpSize : DefaultOtpSize;
        set => otpSize = value >= DefaultOtpSize ? value : DefaultOtpSize;
            }
        

    private readonly IConfiguration _config;
        

    private static readonly char[] AlphanumericChars = "RUHAB24EFGHJKMNPQRSTUVWXYZ23456789".ToCharArray();

    public OtpService(IConfiguration config)
    {
        _config = config;
        otpSize = int.TryParse(_config["otp:Size"],out int size) && size >= 6 ? size:  DefaultOtpSize;
        stepSeconds = int.TryParse(_config["otp:StepSeconds"], out int steps) && steps >= 30 ? steps : DefaultStepsSeconds;

    }

    public (string Otp, DateTime ExpiryTime) GenerateOtp<T>(string secret, T userId, bool alphanumeric = false)
    {
        return GenerateOtp<T>(secret, userId, "default", alphanumeric);
    }

    public (string Otp, DateTime ExpiryTime) GenerateOtp<T>(string secret, T userId, string purpose, bool alphanumeric = false)
    {
        if (string.IsNullOrEmpty(secret)) throw new ArgumentNullException("userId cannot be null");
        if (userId == null) throw new ArgumentNullException("userId cannot be null");

        var dateTime = DateTime.UtcNow;

        var userIdStr = userId.ToString();

        string purposeToLower = purpose.ToLower();

        // totp accept the secret key in bytes so converting the secrets into the bytes
        byte[] secretBytes = GetSecretKeyBytes(secret, userId, purposeToLower);

        // by default OtpHashMode is sha1, changed that into sha256
        var totp = new Totp(secretBytes, StepSeconds, OtpHashMode.Sha256, OtpSize);
        string otp = totp.ComputeTotp(dateTime);

        // return the code on choice ==> CHARS (7HJ9RT) OR DIGITS (603829)
        otp = alphanumeric ? ConvertToAlphaNumericOtp(otp) : otp;

        DateTime expiryTime = dateTime.AddSeconds(StepSeconds);

        return (otp, expiryTime);
    }

    public bool VerifyOtp<T>(string secret, string otp, T userId, bool alphanumeric = false)
    {
        return VerifyOtp<T>(secret, otp, userId, "default", alphanumeric);
    }

    public bool VerifyOtp<T>(string secret, string otp, T userId, string purpose, bool alphanumeric = false)
    {
        if (string.IsNullOrEmpty(secret)) throw new ArgumentNullException("secret cannot be null");
        if (userId == null) throw new ArgumentNullException("userId cannot be null");

        var dateTime = DateTime.UtcNow;

        string purposeToLower = purpose.ToLower();
        // totp accept the secret key in bytes so converting the secrets into the bytes
        byte[] secretBytes = GetSecretKeyBytes(secret, userId, purposeToLower);

        // by default OtpHashMode is sha1, changed that into sha256
        var totp = new Totp(secretBytes, StepSeconds, OtpHashMode.Sha256, OtpSize);

        if (alphanumeric)
        {
            otp = ConvertAlphaNumericOtpToDigits(otp.ToUpper());
        }

        // The parameter `VerificationWindow.RfcSpecifiedNetworkDelay` allows verification within a ±1 time-step range.
        // This means it will check:
        // - The OTP generated at the exact `dateTime` (current step).
        // - The OTP from the previous step (30 seconds before).
        // - The OTP from the next step (30 seconds after).
        // we can customize this behaviour  like this VerificationWindow(previous: 1, future: 1)
        return totp.VerifyTotp(dateTime, otp, out var _, VerificationWindow.RfcSpecifiedNetworkDelay);
    }


    private byte[] GetSecretKeyBytes<T>(string secret, T userId, string purpose)
    {
        string combinedKey = $"{secret}-{userId}-{purpose}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(combinedKey));
    }


    private string ConvertToAlphaNumericOtp(string otp)
    {
        var values = otp.ToCharArray();
        var chars = new char[otp.Length];

        for (int i = 0; i < chars.Length; i++)
        {
            int index = otp[i] - '0';
            chars[i] = AlphanumericChars[index];
        }

        return new string(chars);

    }


    private string ConvertAlphaNumericOtpToDigits(string otp)
    {
        var digits = new char[otp.Length];

        for (int i = 0; i < digits.Length; i++)
        {
            int index = Array.IndexOf(AlphanumericChars, otp[i]); // Find character index
            if (index == -1)
                throw new InvalidOperationException("Invalid alphanumeric OTP character.");

            digits[i] = (char)('0' + index); // Convert back to digit
        }

        return new string(digits);
    }

 


}

