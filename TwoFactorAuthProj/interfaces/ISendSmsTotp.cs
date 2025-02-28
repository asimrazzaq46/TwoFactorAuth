using TwoFactorAuthProj.Dtos;

namespace TwoFactorAuthProj.interfaces;

public interface ISendSmsTotp
{
    Task<bool> MessageAsync(string sendTo, string verificationCode);
}
