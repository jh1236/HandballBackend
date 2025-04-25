using System.Text.Json;
using System.Text.Json.Serialization;

namespace HandballBackend.Converters;

public class NumberConverter : JsonConverter<double> {
    public override void Write(
        Utf8JsonWriter writer,
        double value,
        JsonSerializerOptions options) {
        if (double.IsInfinity(value)) {
            writer.WriteStringValue("\u221e");
        } else if (double.IsNaN(value)) {
            writer.WriteStringValue("-");
        } else {
            writer.WriteNumberValue(value);
        }
    }

    public override double Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) {
        if (reader.TryGetDouble(out var value)) {
            return value;
        }

        var str = reader.GetString();
        return str switch {
            "-" => double.NaN,
            "\u221e" => double.PositiveInfinity,
            _ => throw new JsonException()
        };
    }
}