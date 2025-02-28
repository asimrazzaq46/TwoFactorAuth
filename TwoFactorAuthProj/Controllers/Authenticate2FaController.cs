using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TwoFactorAuthProj.Data;
using TwoFactorAuthProj.Dtos;
using TwoFactorAuthProj.Entities;
using TwoFactorAuthProj.Enums;
using TwoFactorAuthProj.interfaces;

namespace TwoFactorAuthProj.Controllers;

public class Authenticate2FaController(DataContext _context, UserManager<AppUser> _userManager,
ISendEmailTotp _email, ISendSmsTotp _sms,
IOtpService _otpService, IDataBaseOtpService _dbTotp, IOtpGoogleAuth _googleotp) : BaseApicontroller
{



    [HttpPost("generate-otp")]
    public async Task<IActionResult> GenerateOtp(GenerateOtpDto otpDto)
    {

        return await GenerateAuthTypeOtpAsync(otpDto.AuthType, otpDto.Email);

    }


    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp([FromBody] GenerateOtpDto request)
    {

        return await GenerateAuthTypeOtpAsync(request.AuthType, request.Email);

    }





    [HttpPost("verify-otp")]
    public async Task<ActionResult<UserDto>> VerifyOtp(VerifyOtpDto otpDto)
    {
        var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == otpDto.Email);
        if (user == null) return NotFound("user not found");
        var message = string.Empty;

        var method = Enum.TryParse<AuthType>(otpDto.AuthType,true ,out var authMethod);

        //var verifyOtp = await _dbTotp.VerifyOtpAsync(6743,otpDto.Code,OtpPurpose.Login.ToString());

        var verifyOtp = _otpService.VerifyOtp("khsha2432hk", otpDto.Code, user.Id, OtpPurpose.Login.ToString());

        if (!verifyOtp) return BadRequest("Invalid Otp or otp time expires");

        if (method)
        {
            user.AuthType = authMethod;
          await  _context.SaveChangesAsync();
        }

        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email!,
            UserName = user.UserName,
            IsPresistent = user.IsPresistent,
            AuthType = user.AuthType.ToString(),
            PhoneNumber = user.PhoneNumber!
        };

    }

    [HttpPost("genrate-google")]
    public async Task<IActionResult> GoogleAuth(string email)
    {

        var user = await _userManager.FindByEmailAsync(email);

        if (user == null) return NotFound("user not found with this email");

        //TODO create a unique secret for every user and save into database , pass the secret inside GenerateOtp method
        //string secretKey = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16);

        var (qrCodeUrl, manualCode) = _googleotp.GenerateOtp("HrPerf", email);

        if (qrCodeUrl == null || manualCode == null) return BadRequest("Cannot generate google authenticator otp");

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Generate otp", QrUrl = qrCodeUrl, ManualCode = manualCode, Success = true });
    }



    [HttpPost("verify-google")]
    public async Task<ActionResult<UserDto>> VerifyAuthenticatorOtp(VerifyOtpDto otpDto)
    {
        if (otpDto.Email == null || otpDto.Code == null) return BadRequest("email or otp is required");
        var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == otpDto.Email);
        if (user == null || user.Email == null) return NotFound("user not found");

        //TODO Retrive unique secret for google inside user model which is save into database , pass the secret inside VerifyGoogleOtp method

        var verifyOtp = _googleotp.VerifyGoogleOtp(otpDto.Code);

        if (!verifyOtp) return BadRequest("otp has been expired or it's incorrect");

        // saving this method to db for future 2FA
        user.AuthType = AuthType.Google;
       await _context.SaveChangesAsync();

        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            UserName = user.UserName,
            IsPresistent = user.IsPresistent,
            AuthType = user.AuthType.ToString(),
            PhoneNumber = user.PhoneNumber!
        };
    }


    private async Task<IActionResult> GenerateAuthTypeOtpAsync(string authType, string email)
    {
        var message = string.Empty;
        DateTime expiry = default;
        int expirySeconds = 0;

        var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null) return NotFound("user not found");

        if (authType == null) return BadRequest("please select a method to send an otp.");


        if (authType == AuthType.Email.ToString())

        {
            var (resultEmail, ExpiryTime) = await SendEmailTotpAsync(email);

            if (!resultEmail) return BadRequest("Cannot send email");

            message = "otp has been sent to your email";
            expiry = ExpiryTime;
        }

        if (authType == AuthType.Sms.ToString())
        {
            
            var number = user.PhoneNumber;
            var codedNumber = string.Empty;

            if (number is null) return BadRequest("Phone number is not provided");

            var (resultSms, ExpiryTime) = await SendSmsAsync(email, number);
            if (!resultSms) return BadRequest("Cannot send sms");

            var digits = new string(number.Where(char.IsDigit).ToArray());
            if (digits.Length <= 2) codedNumber = digits;
            else codedNumber = string.Concat(new string('X', digits.Length - 2), digits.AsSpan(digits.Length - 2));

            message = $"otp has been sent to your your number {codedNumber}";
            expiry = ExpiryTime;

        }
        var remaingSeconds = (expiry - DateTime.UtcNow).TotalSeconds;
        expirySeconds = (int)Math.Ceiling(remaingSeconds);

      

        return Ok(new { Message = message, Success = true, Expiry = expirySeconds });

    }



    private async Task<(bool success, DateTime date)> SendSmsAsync(string email, string phoneNumber)
    {
        var (code, date) = await GenerateCodeAsync(email);

        //var result =  await _sms.MessageAsync(phoneNumber, code);
        Console.WriteLine(code);

        var result = true;
        return (result, date);
    }


    private async Task<(bool success, DateTime date)> SendEmailTotpAsync(string email)
    {

        var (code, date) = await GenerateCodeAsync(email);
        //var result =  await _email.MessageAsync(email, code);
        var result = true;
        return (result, date);
    }


    private async Task<(string otp, DateTime date)> GenerateCodeAsync(string email)
    {
        var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null) throw new Exception("User not found");

        var (otp, date) = _otpService.GenerateOtp("khsha2432hk", user.Id, OtpPurpose.Login.ToString());

        //var verificationCode = await _dbTotp.GenerateOtp(6743, OtpPurpose.Login.ToString());

        return (otp, date);
    }

}
