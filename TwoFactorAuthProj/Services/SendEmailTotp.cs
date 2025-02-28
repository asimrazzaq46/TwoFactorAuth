using System.Security.Cryptography.X509Certificates;
using SendGrid;
using SendGrid.Helpers.Mail;
using TwoFactorAuthProj.Dtos;
using TwoFactorAuthProj.interfaces;

namespace TwoFactorAuthProj.Services;

public class SendEmailTotp(IConfiguration config) : ISendEmailTotp
{

    private readonly string _apiKey = config["sendGrid:API_KEY_2"] ?? throw new Exception("Cannot connect to an email service");
    private readonly string _fromEmail = config["sendGrid:from"] ?? throw new Exception("Cannot connect to an email service");

    public async Task<bool> MessageAsync(string email,string verificationCode)
    {
        var client = new SendGridClient(_apiKey);
        var from = new EmailAddress(_fromEmail);
        var to = new EmailAddress(email);
        var subject = "verification";
        

        string body = VerificationEmailTemplate(verificationCode);
        var emailMessage = MailHelper.CreateSingleEmail(from, to, subject, "", body);
        var response = await client.SendEmailAsync(emailMessage);

        if (!response.IsSuccessStatusCode) return false;

        return true;
    }

    private string VerificationEmailTemplate(string verificationCode)
    {
        return $@"
      <strong>Verifica il tuo indirizzo email</strong>
    <br/><br/>
    Devi verificare il tuo indirizzo email per continuare a utilizzare il tuo account ---APP_NAME---.
    <br/>
    Inserisci il seguente codice per verificare il tuo indirizzo email:
    <br/><br/>
    <strong>{verificationCode}</strong>
    <br/><br/>
    <br/><br/>
    Se non stavi cercando di accedere al tuo account ---APP_NAME--- e stai vedendo questa email, segui le istruzioni di seguito:
    <ul>
        <li>Reimposta la password del tuo account ---APP_NAME---.</li>
        <li>Controlla se sono state apportate modifiche al tuo account e alle impostazioni utente. In caso affermativo, ripristinale immediatamente.</li>
        <li>Se non riesci ad accedere al tuo account ---APP_NAME---, contatta l'assistenza.</li>
    </ul>
        ";
    }


}


