using System.ComponentModel.DataAnnotations;

namespace Olomove.Contracts.Auth;

public class LoginRequest
{
    [Required, EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }

    public bool RememberMe { get; init; }
}
