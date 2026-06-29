using System.Text.Json.Serialization;

namespace Hyperlaunch.Download;

[JsonConverter(typeof(ArgumentEntryConverter))]
public class ArgumentEntry
{
    #nullable enable
    public string? PlainValue { get; set; }
    public ConditionalArgument? Conditional { get; set; }
    #nullable disable

    public bool IsConditional => Conditional != null;
}