using System.ComponentModel.DataAnnotations;

namespace UserProfile.Api.Features.Auth;

public sealed class RegisterRequest
{
    public const string EmailPattern = @"^[\x21-\x3F\x41-\x7E]+@[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)+$";

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 200 characters.")]
    public string? Name
    {
        get;
        init => field = value?.Trim();
    }

    [Required(ErrorMessage = "Email is required.")]
    [StringLength(320, ErrorMessage = "Email must be at most 320 characters.")]
    [RegularExpression(EmailPattern, ErrorMessage = "Email must be valid.")]
    public string? Email
    {
        get;
        init => field = value?.Trim();
    }

    [Required(AllowEmptyStrings = true, ErrorMessage = "Password is required.")]
    [StringLength(128, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 128 characters.")]
    public string? Password { get; init; }

    [Required(AllowEmptyStrings = true, ErrorMessage = "Password confirmation is required.")]
    [StringLength(128, MinimumLength = 6, ErrorMessage = "Password confirmation must be between 6 and 128 characters.")]
    [Compare(nameof(Password), ErrorMessage = "Password confirmation must match password.")]
    public string? PasswordConfirmation { get; init; }
}
