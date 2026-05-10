using System.Text.Json;
using System.Text.Json.Serialization;

namespace CmsImporter.Infrastructure.Serialization;

/// <summary>
/// Shared <see cref="JsonSerializerOptions"/> used by every serializer/deserializer in the
/// Infrastructure layer (JSONB converters, RabbitMQ event payloads, source-connector parsing).
/// Centralised so JSON shape is consistent across all wire formats.
/// </summary>
internal static class JsonDefaults
{
    /// <summary>
    /// Web defaults — camelCase property names, case-insensitive on read, no indentation,
    /// enums emitted as strings (so consumers see <c>"Page"</c> rather than <c>1</c>).
    /// </summary>
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() },
    };
}
