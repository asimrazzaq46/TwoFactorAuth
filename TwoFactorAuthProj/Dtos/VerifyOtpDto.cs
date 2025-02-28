namespace TwoFactorAuthProj.Dtos;

public class VerifyOtpDto
{

    public required string Email { get; set; }
    public required string Code { get; set; }
    public required string AuthType { get; set; }


}

public class GenerateOtpDto
{

    public required string Email { get; set; }
    public required string AuthType { get; set; }

}



