using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hyperlaunch.Download;

public class ArgumentEntryConverter : JsonConverter<ArgumentEntry>
{
    public override ArgumentEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new ArgumentEntry { PlainValue = reader.GetString() };
        }

        var conditional = JsonSerializer.Deserialize(ref reader, Hyperlaunch.HyperlaunchJsonContext.Default.ConditionalArgument);
        return new ArgumentEntry { Conditional = conditional };
    }

    public override void Write(Utf8JsonWriter writer, ArgumentEntry value, JsonSerializerOptions options)
    {
        if (!value.IsConditional)
            writer.WriteStringValue(value.PlainValue);
        else
            JsonSerializer.Serialize(writer, value.Conditional, Hyperlaunch.HyperlaunchJsonContext.Default.ConditionalArgument);
    }
}