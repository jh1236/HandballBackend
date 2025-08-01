using System.Text.Json;
using System.Text.Json.Serialization;
using HandballBackend.Utils;

namespace HandballBackend.Converters;

public class EnumConverter<T> : JsonConverter<T> where T : struct, Enum {
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var str = reader.GetString();
        if (Enum.TryParse<T>(str?.Replace(" ", ""), ignoreCase: true, out var result)) {
            return result;
        }

        throw new JsonException($"Unable to convert \"{str}\" to enum {typeof(T)}");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) {
        writer.WriteStringValue(Utilities.SplitCamelCase(value.ToString()));
    }
}