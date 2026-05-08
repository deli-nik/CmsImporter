using CmsImporter.Core.DTOs;

namespace CmsImporter.Core.Abstractions;

public interface ISourceConnector
{
    string Name { get; }

    IAsyncEnumerable<RawContent> ReadAsync(
        SourceConnectorOptions options,
        CancellationToken cancellationToken = default);
}

public sealed record SourceConnectorOptions
{
    public required IReadOnlyDictionary<string, string> Settings { get; init; }

    public static SourceConnectorOptions Empty { get; } =
        new() { Settings = new Dictionary<string, string>() };

    public string Require(string key) =>
        Settings.TryGetValue(key, out var value)
            ? value
            : throw new ArgumentException(
                $"Source connector option '{key}' is required.", nameof(key));

    public string? GetOrDefault(string key) =>
        Settings.TryGetValue(key, out var value) ? value : null;
}
