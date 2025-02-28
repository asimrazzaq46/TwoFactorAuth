using TwoFactorAuthProj.Dtos;

namespace TwoFactorAuthProj.interfaces;

public interface ISendEmailTotp
{

    Task<bool> MessageAsync(string email, string verificationCode);
}

