using CmsImporter.Core.Entities;

namespace CmsImporter.Application.Pipeline;

public sealed class ValidateStage
{
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
