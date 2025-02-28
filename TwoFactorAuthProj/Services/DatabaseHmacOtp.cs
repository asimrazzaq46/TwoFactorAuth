using System.Security.Cryptography;
using System.Text;
using OtpNet;
using TwoFactorAuthProj.Entities;
using TwoFactorAuthProj.Enums;
using TwoFactorAuthProj.interfaces;

namespace TwoFactorAuthProj.Services;

public class DatabaseHmacOtp(IOtpRepositery _dbOTP, IConfiguration _config) : IDataBaseOtpService
{
    private const int OtpSize = 6; // create 6-digit code
    private const int MaxFailedAttempts = 5;
    private const int LockoutTimeInMinutes = 10;
    private const int stepSeconds = 30;
    private readonly OtpHashMode OtpHashMode = OtpHashMode.Sha256;
    private readonly string secret = _config["HOTP:secret"] ?? throw new NullReferenceException($"Please provaide a HOTP : {nameof(secret)} in your application app setting");
    private static readonly char[] AlphanumericChars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789".ToCharArray();

    public async Task<string> GenerateOtp<T>( T userid, string purpose, bool alphanumeric = false)
    {
        if (userid == null) throw new ArgumentNullException("userid cannot be null");
        if (!Enum.TryParse<OtpPurpose>(purpose, true, out var otpPurpose)) throw new ArgumentNullException("purpose cannot be null");

        string useridString = userid.ToString() ?? throw new ArgumentNullException("userId cannot be null");

        var failedAttempt = await _dbOTP.GetFailedAttemptAsync(useridString);

        if (failedAttempt != null && failedAttempt.LockoutExpiration > DateTime.UtcNow) throw new Exception("user is locked out");

        await _dbOTP.RemoveOtpByUserIdAndPurpose(useridString, otpPurpose);


        var secretKey = GenerateSecretKey(secret, userid, purpose);

        var hotp = new Hotp(secretKey, OtpHashMode, OtpSize);

        long counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / stepSeconds;

        string otp =  hotp.ComputeHOTP(counter);

        otp = alphanumeric ? ConvertToAlphanumeric(otp) : otp;



        var expiry = DateTime.UtcNow.AddSeconds(stepSeconds);

        var otpRecord = new OtpRecord
        {
            Otp = otp,
            Purpose = otpPurpose,
            Expiry = expiry,
            UserId = useridString
        };


        await _dbOTP.AddOtpAsync(otpRecord);
        await _dbOTP.SavechangesAsync();

        return otp;


    }

    public async Task<bool> VerifyOtpAsync<T>(T userId, string otp, string purpose)
    {
        if (userId == null) throw new ArgumentNullException("userId cannot be null");
        if (!Enum.TryParse<OtpPurpose>(purpose, true, out var otpPurpose)) throw new ArgumentNullException("purpose cannot be null");

        var userIdString = userId.ToString() ?? throw new ArgumentNullException("userId cannot be null");

        var otpinDb = await _dbOTP.GetOtpAsync(userIdString, otpPurpose);
        if (otpinDb == null || otpinDb.Otp != otp || otpinDb.Expiry <= DateTime.UtcNow.AddMilliseconds(-50))
        {
            await IncrementFailedAttemptsAsync(userIdString);
            return false;
        }


        var secretKey = GenerateSecretKey(secret,userId,purpose);

        var hotp = new Hotp(secretKey, OtpHashMode, OtpSize);

        long counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / stepSeconds;

        bool isOtpValid = hotp.VerifyHotp(otp, counter);

        if (!isOtpValid) {

            await IncrementFailedAttemptsAsync(userIdString);
            return false;
        }


        _dbOTP.RemoveOtp(otpinDb);
        await _dbOTP.ResetFailedAttemptsAsync(userIdString);

        await _dbOTP.SavechangesAsync();

        return true;


    }

    private string ConvertToAlphanumeric(string otp)
    {
        int values = int.Parse(otp);
        var chars = new char[otp.Length];

        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = AlphanumericChars[values % AlphanumericChars.Length];
            values /= AlphanumericChars.Length;
        }

        return new string(chars);
    }

    private async Task IncrementFailedAttemptsAsync(string userIdString)
    {
        var attempt = await _dbOTP.GetFailedAttemptAsync(userIdString);
        if (attempt == null)
        {
            attempt = new OtpFailedAttempt
            {
                UserId = userIdString,
                FailedAttemptsCount = 1,
            };
            await _dbOTP.AddFailedAttemptAsync(attempt);
        }
        else
        {
            attempt.FailedAttemptsCount++;
            if (attempt.FailedAttemptsCount >= MaxFailedAttempts)
            {
                await _dbOTP.LockOutUserAsync(userIdString,DateTime.UtcNow.AddMinutes(LockoutTimeInMinutes));
            }

        }

        await _dbOTP.SavechangesAsync();
    }

    private byte[] GenerateSecretKey<T>(string secret, T userid, string purpose)
    {
        var combineHashKey = Encoding.UTF8.GetBytes($"{secret}-{userid}-{purpose}");
        var hashSecret = Encoding.UTF8.GetBytes(secret);

        var hmac = new HMACSHA256(hashSecret);

        return hmac.ComputeHash(combineHashKey);

    }
}
