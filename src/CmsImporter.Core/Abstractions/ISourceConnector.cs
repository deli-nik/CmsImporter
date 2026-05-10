using CmsImporter.Core.DTOs;

namespace CmsImporter.Core.Abstractions;

/// <summary>
/// Adapter for a source CMS. Each implementation knows how to stream content out of one specific
/// source format/transport (file system, REST, RSS, ...). Resolved by <see cref="Name"/> via
/// the <c>ISourceConnectorRegistry</c>.
/// </summary>
public interface ISourceConnector
{
    /// <summary>Stable identifier used by callers to select this connector (e.g., "FileSystem").</summary>
    string Name { get; }

    /// <summary>
    /// Streams content items from the source. Implementations must yield lazily so
    /// arbitrarily large exports import in bounded memory.
    /// </summary>
    /// <param name="options">Per-job configuration (paths, URLs, source-system tag, ...).</param>
    /// <param name="cancellationToken">Honors cooperative cancellation; should flow into any
    /// underlying stream/HTTP reads.</param>
    IAsyncEnumerable<RawContent> ReadAsync(
        SourceConnectorOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Per-job configuration passed to a connector. Free-form key/value bag so adding a new connector
/// doesn't require widening this type — the connector validates the keys it cares about via
/// <see cref="Require"/> / <see cref="GetOrDefault"/>.
/// </summary>
public sealed record SourceConnectorOptions
{
    /// <summary>The raw key/value settings as supplied by the caller (typically the API request body).</summary>
    public required IReadOnlyDictionary<string, string> Settings { get; init; }

    /// <summary>An empty options bag — useful for tests and as a default.</summary>
    public static SourceConnectorOptions Empty { get; } =
        new() { Settings = new Dictionary<string, string>() };

    /// <summary>Returns the value for <paramref name="key"/>, or throws if it's missing.</summary>
    /// <exception cref="ArgumentException">Thrown when the key is not present in <see cref="Settings"/>.</exception>
    public string Require(string key) =>
        Settings.TryGetValue(key, out var value)
            ? value
            : throw new ArgumentException(
                $"Source connector option '{key}' is required.", nameof(key));

    /// <summary>Returns the value for <paramref name="key"/>, or <see langword="null"/> if missing.</summary>
    public string? GetOrDefault(string key) =>
        Settings.TryGetValue(key, out var value) ? value : null;
}
