using CmsImporter.Core.Entities;

namespace CmsImporter.Application.Pipeline;

/// <summary>
/// Pure per-item validation. Returns a <see cref="ValidationResult"/> describing every rule
/// the item failed (rather than failing fast on the first one) so a single-pass validator can
/// surface all issues to the caller in one go.
/// </summary>
public sealed class ValidateStage
{
    /// <summary>
    /// Checks the candidate against the importer's required-field and length rules.
    /// Returns <see cref="ValidationResult.Valid"/> when nothing is wrong.
    /// </summary>
    public ValidationResult Execute(ContentItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(item.ExternalId))
        {
            errors.Add("ExternalId is required.");
        }

        if (string.IsNullOrWhiteSpace(item.SourceSystem))
        {
            errors.Add("SourceSystem is required.");
        }

        if (string.IsNullOrWhiteSpace(item.Title))
        {
            errors.Add("Title is required.");
        }

        if (item.Type == ContentType.Unknown)
        {
            errors.Add("Type could not be resolved to a known ContentType.");
        }

        if (item.Title.Length > 500)
        {
            errors.Add("Title exceeds 500 characters.");
        }

        return errors.Count == 0
            ? ValidationResult.Valid
            : ValidationResult.Invalid([.. errors]);
    }
}
