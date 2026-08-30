using System.ComponentModel.DataAnnotations;

namespace UserProfile.Api.Features.Auth;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [StringLength(320, ErrorMessage = "Email must be at most 320 characters.")]
    [RegularExpression(RegisterRequest.EmailPattern, ErrorMessage = "Email must be valid.")]
    public string? Email
    {
        get;
        init => field = value?.Trim();
    }

    [Required(AllowEmptyStrings = true, ErrorMessage = "Password is required.")]
    [StringLength(128, MinimumLength = 1, ErrorMessage = "Password must be between 1 and 128 characters.")]
    public string? Password { get; init; }
}
