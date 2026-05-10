namespace CmsImporter.Application.Pipeline;

/// <summary>
/// The outcome of a per-item validation check. Use the <see cref="Valid"/> singleton or
/// <see cref="Invalid"/> factory rather than constructing instances directly.
/// </summary>
public sealed record ValidationResult
{
    /// <summary><see langword="true"/> when the item passed all checks.</summary>
    public required bool IsValid { get; init; }

    /// <summary>Human-readable error messages — empty when <see cref="IsValid"/> is true.</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>The shared "passed" result; reused across calls to avoid allocation.</summary>
    public static ValidationResult Valid { get; } = new() { IsValid = true };

    /// <summary>Builds a failed result with the given error messages.</summary>
    public static ValidationResult Invalid(params string[] errors) =>
        new() { IsValid = false, Errors = errors };
}
