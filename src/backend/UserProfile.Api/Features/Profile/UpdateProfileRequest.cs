using System.ComponentModel.DataAnnotations;
using UserProfile.Api.Features.Auth;

namespace UserProfile.Api.Features.Profile;

public sealed class UpdateProfileRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 200 characters.")]
    public string? Name
    {
        get;
        init => field = value?.Trim();
    }

    [Required(ErrorMessage = "Email is required.")]
    [StringLength(320, ErrorMessage = "Email must be at most 320 characters.")]
    [RegularExpression(RegisterRequest.EmailPattern, ErrorMessage = "Email must be valid.")]
    public string? Email
    {
        get;
        init => field = value?.Trim();
    }
}
