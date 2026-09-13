using System.ComponentModel.DataAnnotations;

namespace TokenAuthDemo.Dtos;

public record RegisterRequest(
    [Required, MinLength(3), MaxLength(32)] string Username,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password
);

/// <summary>
/// Returned on successful registration. No tokens here — creating an account and
/// obtaining a token are now separate, standard OAuth2 steps: register once via
/// this endpoint, then call POST /connect/token (grant_type=password) to log in.
/// </summary>
public record RegisterResponse(string UserId, string Username, string Email);

public record ErrorResponse(string Message);
