using System.Text.Json;
using System.Text.Json.Serialization;

namespace NestyStay.Infrastructure.Persistence.Milestones;

internal static class MilestoneJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static List<T> DeserializeList<T>(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<T>>(json, Options) ?? [];
}
