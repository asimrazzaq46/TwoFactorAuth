using TwoFactorAuthProj.Dtos;

namespace TwoFactorAuthProj.interfaces;

public interface ITokenService
{
    Task<string> Createtoken(UserDto user);
}
