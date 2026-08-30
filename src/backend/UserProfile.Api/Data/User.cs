namespace UserProfile.Api.Data;

// SQLite stores these values as TEXT and ignores declared length facets. Request DTOs
// enforce the bounded inputs, while PasswordHasher owns the hash representation.
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength
public sealed class User
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
// ReSharper restore EntityFramework.ModelValidation.UnlimitedStringLength
