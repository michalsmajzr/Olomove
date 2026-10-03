using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Olomove.Contracts.Auth;
using Olomove.Models;

namespace Olomove.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<User> userManager,
    SignInManager<User> signInManager) : ControllerBase
{
    private async Task<object> ToResponse(User user) => new
    {
        user.Id,
        user.Email,
        user.FirstName,
        user.LastName,
        Roles = await userManager.GetRolesAsync(user)
    };

    // create a local account with an email address and password
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(
                result.Errors.ToDictionary(error => error.Code, error => new[] { error.Description })));
        }

        var roleResult = await userManager.AddToRoleAsync(user, "User");
        if (!roleResult.Succeeded)
        {
            return Problem("Uživatelskému účtu se nepodařilo přiřadit výchozí roli.");
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return Created("/api/auth/me", await ToResponse(user));
    }

    // sign in an existing local account with email and password
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "E-mail nebo heslo není správné." });
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized();
        }
        return Ok(await ToResponse(user));
    }

    // remove the current authentication cookie
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    // return the user identified by the current authentication cookie
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await userManager.GetUserAsync(User);
        return user is null ? Unauthorized() : Ok(await ToResponse(user));
    }

    
}
