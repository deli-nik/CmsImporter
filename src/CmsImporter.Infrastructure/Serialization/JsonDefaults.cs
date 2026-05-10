using System.Text.Json;
using System.Text.Json.Serialization;

namespace CmsImporter.Infrastructure.Serialization;

internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() },
    };
}
