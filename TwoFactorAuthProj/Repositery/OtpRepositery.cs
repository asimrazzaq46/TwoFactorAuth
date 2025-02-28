using Microsoft.EntityFrameworkCore;
using TwoFactorAuthProj.Data;
using TwoFactorAuthProj.Entities;
using TwoFactorAuthProj.Enums;
using TwoFactorAuthProj.interfaces;

namespace TwoFactorAuthProj.Repositery
{
    public class OtpRepositery(DataContext _db) : IOtpRepositery
    {
        public async Task AddFailedAttemptAsync(OtpFailedAttempt failedAttempt)
        {
            await _db.OtpFailedAttempts.AddAsync(failedAttempt);
        }

        public async Task AddOtpAsync(OtpRecord otpRecord)
        {
            await _db.OtpRecords.AddAsync(otpRecord);
        }

        public async Task<OtpFailedAttempt?> GetFailedAttemptAsync(string userId)
        {
            return await _db.OtpFailedAttempts.FirstOrDefaultAsync(fa => fa.UserId == userId);
        }

        public async Task<OtpRecord> GetOtpAsync(string userId, OtpPurpose purpose)
        {
            return await _db.OtpRecords.FirstOrDefaultAsync(o => o.UserId.Equals(userId) && o.Purpose == purpose) ?? throw new Exception("Otp not found");
        }

        public async Task LockOutUserAsync(string userId, DateTime lockoutExpiration)
        {
            var failedAttemp = await GetFailedAttemptAsync(userId);
            if (failedAttemp == null)
            {
                //failedAttemp = new OtpFailedAttempt
                //{
                //    UserId = userId,
                //    FailedAttemptsCount = 1,
                //};
                //await AddFailedAttemptAsync(failedAttemp);
                throw new Exception($"cannot lockout this user because it's his first attempt.");
            }
            else
            {
                failedAttemp.LockoutExpiration = lockoutExpiration;
            }
        }

        public void RemoveOtp(OtpRecord otpRecord)
        {
            _db.OtpRecords.Remove(otpRecord);
        }

        public async Task RemoveOtpByUserIdAndPurpose(string userId, OtpPurpose purpose)
        {
            var existingRecords = _db.OtpRecords
            .Where(o => o.UserId == userId && o.Purpose == purpose);
            if (existingRecords.Any())
            {
                _db.OtpRecords.RemoveRange(existingRecords);
                await _db.SaveChangesAsync();
            }
        }

        public async Task ResetFailedAttemptsAsync(string userId)
        {
            var failedAttempt = await GetFailedAttemptAsync(userId);
            if (failedAttempt != null)
            {
                failedAttempt.FailedAttemptsCount = 0;
                failedAttempt.LockoutExpiration = null;
                await SavechangesAsync();
            }
        }

        public async Task SavechangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
