using Twilio.Types;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using TwoFactorAuthProj.Dtos;
using TwoFactorAuthProj.interfaces;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TwoFactorAuthProj.Services
{
    public class SendSmsTotp(IConfiguration _config) : ISendSmsTotp
    {
        private readonly string _accountSid = _config["twilio:accountsid"] ?? throw new Exception("account sid is not provided");
        private readonly string _authtoken = _config["twilio:authToken"] ?? throw new Exception("account token is not provided");
        private readonly string _appNumber = _config["twilio:appNumber"] ?? throw new Exception("Admin number is not provided");

        public async Task<bool> MessageAsync(string sendTo,string verificationCode)
        {

            var body = GetVerificationSmsTemplate(verificationCode);
            try
            {
                 TwilioClient.Init(_accountSid, _authtoken);

                var sms = await MessageResource.CreateAsync(
                    body: body,
                    from: new PhoneNumber(_appNumber),
                    to: new PhoneNumber(sendTo)
                );

                return sms.ErrorCode == null; // Return true if sent successfully
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Twilio SMS Error: {ex.Message}");
                return false;
            }
        }

        private static string GetVerificationSmsTemplate(string verificationCode)
        {
            return $@"
    Il tuo codice di verifica per ---APP_NAME--- è: {verificationCode}
    ";
        }
    }
}
