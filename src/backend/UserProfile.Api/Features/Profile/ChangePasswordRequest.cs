using System.ComponentModel.DataAnnotations;

namespace UserProfile.Api.Features.Profile;

public sealed class ChangePasswordRequest
{
    [Required(AllowEmptyStrings = true, ErrorMessage = "Current password is required.")]
    [StringLength(128, MinimumLength = 1, ErrorMessage = "Current password must be between 1 and 128 characters.")]
    public string? CurrentPassword { get; init; }

    [Required(AllowEmptyStrings = true, ErrorMessage = "New password is required.")]
    [StringLength(128, MinimumLength = 6, ErrorMessage = "New password must be between 6 and 128 characters.")]
    public string? NewPassword { get; init; }

    [Required(AllowEmptyStrings = true, ErrorMessage = "New password confirmation is required.")]
    [StringLength(128, MinimumLength = 6, ErrorMessage = "New password confirmation must be between 6 and 128 characters.")]
    [Compare(nameof(NewPassword), ErrorMessage = "New password confirmation must match new password.")]
    // Bound and validated by ASP.NET Core before the controller executes.
    // ReSharper disable once UnusedMember.Global
    public string? NewPasswordConfirmation { get; init; }
}
