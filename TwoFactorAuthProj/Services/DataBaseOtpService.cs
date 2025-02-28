using System.Security.Cryptography;
using System.Text;
using TwoFactorAuthProj.Entities;
using TwoFactorAuthProj.Enums;
using TwoFactorAuthProj.interfaces;

namespace TwoFactorAuthProj.Services;

public class DataBaseOtpService(IOtpRepositery _dbOTP,IConfiguration _config) : IDataBaseOtpService
{
    private static ReadOnlySpan<char> AlphanumericChars => "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int MaxPossibleFailedAttempt = 5;

    private const int LOCKOUT_TIME_IN_MINUTES = 10;
    private readonly string secret = _config["HOTP:secret"] ?? throw new NullReferenceException($"Please provaide a HOTP : {nameof(secret)} in your application app setting");


    public async Task<string> GenerateOtp<T>(T userid, string purpose, bool alphanumeric = false)
    {
        //validate user id and otpPurpose
        if (userid == null) throw new ArgumentNullException("userid is required");

        if (!Enum.TryParse<OtpPurpose>(purpose, true, out var otpPurpose)) throw new Exception($"Invalid OTP purpose: {purpose}");

        string userIdString = userid.ToString() ?? throw new Exception("userid is null");

        // Check lockout status
        var failedAttemp = await _dbOTP.GetFailedAttemptAsync(userIdString);

        if(failedAttemp != null)
        {
        if (failedAttemp.LockoutExpiration > DateTime.UtcNow) throw new Exception("user is locked out");
        }

        //before generating new otp's, invalidate the previous one
       await _dbOTP.RemoveOtpByUserIdAndPurpose(userIdString, otpPurpose);


        // checking the otp if user wants in numeric form like (758432) or in string form (YCRHIL)
        string otp = alphanumeric ? GenerateAlphanumericOtp(secret, purpose) : GenerateNumericOtp(secret,purpose);

        // after generating otp we save that otp in db
        var otpToCreate = new OtpRecord
        {
            Otp = otp,
            Purpose = otpPurpose,
            UserId = userIdString,
            Expiry = DateTime.UtcNow.AddMinutes(5).AddTicks(-(DateTime.UtcNow.Ticks % TimeSpan.TicksPerSecond)),

        };

        await _dbOTP.AddOtpAsync(otpToCreate);

        await _dbOTP.SavechangesAsync();

        return otp;


    }

    public async Task<bool> VerifyOtpAsync<T>(T userId, string otp, string purpose)
    {
        //validate user id and otpPurpose
        if (userId == null) throw new ArgumentNullException("userid is null");
        if (!Enum.TryParse<OtpPurpose>(purpose, true, out var otpPurpose)) throw new Exception($"Invalid OTP purpose: {purpose}");

        string userIdString = userId.ToString() ?? throw new Exception("userid is null");

        var otpInDb = await _dbOTP.GetOtpAsync(userIdString, otpPurpose);

        // if otp we have saved inside database is not valid then wee increment the Failed attempt
        if (otpInDb == null || otpInDb.Otp != otp || otpInDb.Expiry <= DateTime.UtcNow.AddMilliseconds(-50))
        {
            await IncrementFailedAttemptsAsync(userIdString);
            return false;
        }

        //if itp is valid then we can remove the otp from database and reset the attempts for the user 
        _dbOTP.RemoveOtp(otpInDb);
        await _dbOTP.ResetFailedAttemptsAsync(userIdString);

        return true;

    }

    private async Task IncrementFailedAttemptsAsync(string userIdStr)
    {
        var attempt = await _dbOTP.GetFailedAttemptAsync(userIdStr);

        // if it's user first failed attemp then we create a new OtpFailedAttempt object and save into db
        if (attempt == null)
        {
            attempt = new OtpFailedAttempt
            {
                UserId = userIdStr,
                FailedAttemptsCount = 1,
            };
        }
        else
        {
            // if user have already tried a failed attemp then we increment by 1
            attempt.FailedAttemptsCount++;

            // if user have reached the max limit then we are going to lockout the user for 10 minutes
            if (attempt.FailedAttemptsCount >= MaxPossibleFailedAttempt)
            {
                await _dbOTP.LockOutUserAsync(userIdStr, DateTime.UtcNow.AddMinutes(LOCKOUT_TIME_IN_MINUTES));
            }
        }

       await _dbOTP.SavechangesAsync();
    }

    private string GenerateNumericOtp(string secret,string purpose)
    {

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));

        var timeStamp = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
        var timestampToString = Convert.ToBase64String(timeStamp);

        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{purpose}-{timestampToString}"));

        int offset = hash[^1] & 0xF;
        int binaryCode = BitConverter.ToInt32(hash, offset) & 0x7FFFFFFF;


        return (binaryCode % 1_000_000).ToString("D6");

    }

    private string GenerateAlphanumericOtp(string secret, string purpose)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));

        var timeStamp = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
        var timestampToString = Convert.ToBase64String(timeStamp);

        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{purpose}-{timestampToString}"));

        Span<char> otp = stackalloc char[6];

        for (int i = 0; i < otp.Length; i++)
        {
            // % AlphanumericChars.Length ensures the index is always within the valid range of AlphanumericChars (0–31)
            otp[i] = AlphanumericChars[hash[i] % AlphanumericChars.Length]; // Map hash byte to char
        }

        return new string(otp);

    }


}
