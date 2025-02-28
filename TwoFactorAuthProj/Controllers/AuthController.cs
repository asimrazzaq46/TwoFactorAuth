using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TwoFactorAuthProj.Data;
using TwoFactorAuthProj.Dtos;
using TwoFactorAuthProj.Entities;

namespace TwoFactorAuthProj.Controllers;

public class AuthController(DataContext _context, UserManager<AppUser> _userManager) : BaseApicontroller
{
    [HttpPost("register")]
    public async Task<ActionResult<AppUser>> Register(Registerdto register)
    {

        var user = new AppUser
        {

            Name = register.Name,
            Email = register.Email,
            UserName = register.Username,
            PhoneNumber = register.PhoneNumber,
        };
        var result = await _userManager.CreateAsync(user, register.Password);

        if (!result.Succeeded) return BadRequest(result.Errors);


        return user;

    }


    [HttpPost("login")]
    public async Task<ActionResult> Login(LoginDto login)
    {

        if (string.IsNullOrEmpty(login.Email) || string.IsNullOrEmpty(login.Password)) return BadRequest("email and Passsword is required");
        var user = await _userManager.FindByEmailAsync(login.Email);



        if (user == null) return NotFound("User with this email is not exist");

        var result = await _userManager.CheckPasswordAsync(user, login.Password);

        if (!result) return BadRequest("Email or password is incorrect");


        return Ok(new { user.Email, Success = true, AuthType = user.AuthType.ToString() });
    }



   


    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _context.AppUsers.FindAsync(id);
  
        if (user == null) return NotFound("User is not found");

        _context.AppUsers.Remove(user);

        await _context.SaveChangesAsync();


        return Ok();

    }

    [HttpPut("update/{id}")]
    public async Task<ActionResult<UserDto>> UpdateUser(string id,UpdateDto data)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null) return NotFound("user not found with this id");

        if (!string.IsNullOrWhiteSpace(data.Name))
            user.Name = data.Name;

        if (!string.IsNullOrWhiteSpace(data.UserName))
            user.UserName = data.UserName;

        if (!string.IsNullOrWhiteSpace(data.PhoneNumber))
            user.PhoneNumber = data.PhoneNumber;

        if (data.AuthType.HasValue)
        {
            user.AuthType = data.AuthType.Value;
        }
        else
        {
            user.AuthType = null;  // or save it as null
        }

        if (data.IsPresistent.HasValue)
            user.IsPresistent = data.IsPresistent.Value;

        

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest(result.Errors);

        // Return the updated user data
        var userDto = new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            UserName = user.UserName,
            PhoneNumber = user.PhoneNumber,
            AuthType = user.AuthType.ToString(),
            IsPresistent = user.IsPresistent,
            Email = user.Email,
            VerificationCode = user.VerificationCode,

        };

        return Ok(userDto);
    }
    


    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(string id)
    {

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound("user with this id not exist");

        return new UserDto
        {

            Email = user.Email!,
            Id = user.Id,
            Name = user.Name,
            PhoneNumber = user.PhoneNumber!,
            VerificationCode = user.VerificationCode,
            AuthType = user.AuthType.ToString(),
            IsPresistent = user.IsPresistent
        };
    }
}
