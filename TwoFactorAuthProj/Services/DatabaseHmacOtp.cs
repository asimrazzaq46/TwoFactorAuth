using System.Security.Cryptography;
using System.Text;
using OtpNet;
using TwoFactorAuthProj.Entities;
using TwoFactorAuthProj.interfaces;

namespace TwoFactorAuthProj.Services;

public class DatabaseHmacOtp(IOtpRepositery _dbOTP, IConfiguration _config) : IDataBaseOtpService
{
    private const int OtpSize = 6; // create 6-digit code
    private const int MaxFailedAttempts = 5;
    private const int LockoutTimeInMinutes = 1;
    private const int stepSeconds = 60;
    private const int VerificationWindow = 2;
    private readonly OtpHashMode OtpHashMode = OtpHashMode.Sha256;
    private readonly string secret = _config["HOTP:secret"] ?? throw new NullReferenceException($"Please provaide a HOTP : {nameof(secret)} in your application app setting");
    private static readonly char[] AlphanumericChars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789".ToCharArray();

    public async Task<(string otp,DateTime expiry)> GenerateOtp<T>( T userid, string purpose, bool alphanumeric = false)
    {
        if (userid == null) throw new ArgumentNullException("userid cannot be null");
        //if (!Enum.TryParse<OtpPurpose>(purpose, true, out var otpPurpose)) throw new ArgumentNullException("purpose cannot be null");

        string useridString = userid.ToString() ?? throw new ArgumentNullException("userId cannot be null");

        var failedAttempt = await _dbOTP.GetFailedAttemptAsync(useridString);

        if (failedAttempt != null && failedAttempt.LockoutExpiration > DateTime.UtcNow) throw new Exception("user is locked out");

        await _dbOTP.RemoveOtpByUserIdAndPurpose(useridString, purpose);


        var secretKey = GenerateSecretKey(secret, userid, purpose);

        var hotp = new Hotp(secretKey, OtpHashMode, OtpSize);

        //var lastCounter = await _dbOTP.GetLastUsedCounterAsync(useridString) ?? 1;

        long counter = DateTime.UtcNow.Ticks / (TimeSpan.TicksPerSecond * stepSeconds);

        string otp =  hotp.ComputeHOTP(counter);

        otp = alphanumeric ? ConvertToAlphanumeric(otp) : otp;



        var expiry = DateTime.UtcNow.AddSeconds(stepSeconds);

        var otpRecord = new OtpRecord
        {
            Otp = otp,
            Purpose = purpose,
            Expiry = expiry,
            UserId = useridString,
            Counter = counter,
            
        };


        await _dbOTP.AddOtpAsync(otpRecord);
        await _dbOTP.SavechangesAsync();

        return (otp,expiry);


    }

    public async Task<bool> VerifyOtpAsync<T>(T userId, string otp, string purpose)
    {
        if (userId == null) throw new ArgumentNullException("userId cannot be null");
        //if (!Enum.TryParse<OtpPurpose>(purpose, true, out var otpPurpose)) throw new ArgumentNullException("purpose cannot be null");

        var userIdString = userId.ToString() ?? throw new ArgumentNullException("userId cannot be null");


        var otpinDb = await _dbOTP.GetOtpAsync(userIdString, purpose);


        if (otpinDb == null || otpinDb.Otp != otp || otpinDb.Expiry <= DateTime.UtcNow.AddMilliseconds(-50))
        {
            await IncrementFailedAttemptsAsync(userIdString);
            return false;
        }

        var secretKey = GenerateSecretKey(secret,userId,purpose);

        var hotp = new Hotp(secretKey, OtpHashMode, OtpSize);

        long counter = otpinDb.Counter;
    
        bool isOtpValid = false;
        for (int i = -VerificationWindow; i<=VerificationWindow; i++){
            var testCounter = counter + i;
            Console.WriteLine($"Testing counter: {testCounter} with OTP: {otp}");
            if (hotp.VerifyHotp(otp,counter + i))
            {
                isOtpValid = true;
                counter += i;
                break;
            }
        }

        if (!isOtpValid) {

            await IncrementFailedAttemptsAsync(userIdString);
            return false;
        }

        //await _dbOTP.UpdateCounterAsync(userIdString, counter);

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
            if (attempt.LockoutExpiration.HasValue && attempt.LockoutExpiration.Value > DateTime.UtcNow)
            {
                // If the lockout period is not yet expired, throw an exception
                throw new Exception("Try again after a few minutes. User is locked out.");
            }


            // Check if the lockout period has expired
            if (attempt.LockoutExpiration.HasValue && attempt.LockoutExpiration.Value <= DateTime.UtcNow.AddMinutes(-LockoutTimeInMinutes))
            {
                // Reset failed attempts if the lockout period has expired
                attempt.FailedAttemptsCount = 0;
                attempt.LockoutExpiration = null;
            }

            attempt.FailedAttemptsCount++;
            if (attempt.FailedAttemptsCount >= MaxFailedAttempts)
            {
                await _dbOTP.LockOutUserAsync(userIdString, DateTime.UtcNow.AddMinutes(LockoutTimeInMinutes));
               
            }

            await _dbOTP.SavechangesAsync();
        }
    }

    private byte[] GenerateSecretKey<T>(string secret, T userid, string purpose)
    {
        var combineHashKey = Encoding.UTF8.GetBytes($"{secret}-{userid}-{purpose}");
        var hashSecret = Encoding.UTF8.GetBytes(secret);

        var hmac = new HMACSHA256(hashSecret);

        return hmac.ComputeHash(combineHashKey);

    }
}
