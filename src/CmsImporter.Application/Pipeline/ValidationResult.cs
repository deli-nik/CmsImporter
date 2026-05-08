namespace CmsImporter.Application.Pipeline;

public sealed record ValidationResult
{
    public required bool IsValid { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public static ValidationResult Valid { get; } = new() { IsValid = true };

    public static ValidationResult Invalid(params string[] errors) =>
        new() { IsValid = false, Errors = errors };
}
