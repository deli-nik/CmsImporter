using System.Text.Json;

using CmsImporter.Core.ValueObjects;
using CmsImporter.Infrastructure.Serialization;

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CmsImporter.Infrastructure.Persistence;

internal sealed class ContentBodyJsonConverter : ValueConverter<ContentBody, string>
{
    public ContentBodyJsonConverter()
        : base(
            body => JsonSerializer.Serialize(body, JsonDefaults.Web),
            json => JsonSerializer.Deserialize<ContentBody>(json, JsonDefaults.Web)!)
    {
    }
}

internal sealed class ContentBodyValueComparer : ValueComparer<ContentBody>
{
    public ContentBodyValueComparer()
        : base(
            (a, b) => a == b,
            body => body == null ? 0 : body.GetHashCode(),
            body => body)
    {
    }
}

internal sealed class StringDictionaryJsonConverter
    : ValueConverter<IReadOnlyDictionary<string, string>, string>
{
    public StringDictionaryJsonConverter()
        : base(
            dict => JsonSerializer.Serialize(dict, JsonDefaults.Web),
            json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonDefaults.Web)
                ?? new Dictionary<string, string>())
    {
    }
}

internal sealed class StringDictionaryValueComparer
    : ValueComparer<IReadOnlyDictionary<string, string>>
{
    public StringDictionaryValueComparer()
        : base(
            (a, b) => CompareDicts(a, b),
            dict => dict == null
                ? 0
                : dict.Aggregate(0, (acc, kv) => HashCode.Combine(acc, kv.Key, kv.Value)),
            dict => dict == null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(dict))
    {
    }

    private static bool CompareDicts(
        IReadOnlyDictionary<string, string>? left,
        IReadOnlyDictionary<string, string>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        foreach (var kv in left)
        {
            if (!right.TryGetValue(kv.Key, out var v) || v != kv.Value)
            {
                return false;
            }
        }

        return true;
    }
}
