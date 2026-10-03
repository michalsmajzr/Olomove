using System.ComponentModel.DataAnnotations;

namespace Olomove.Contracts.Auth;

public class RegisterRequest
{
    [Required, StringLength(100)]
    public required string FirstName { get; init; }

    [Required, StringLength(100)]
    public required string LastName { get; init; }

    [Required, EmailAddress]
    public required string Email { get; init; }

    [Required, StringLength(100, MinimumLength = 8)]
    public required string Password { get; init; }
}
