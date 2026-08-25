using System.ComponentModel.DataAnnotations;

namespace UserProfile.Api.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required(AllowEmptyStrings = false)]
    public required string Default { get; init; }
}
