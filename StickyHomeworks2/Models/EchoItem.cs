using System.Text.Json.Serialization;

namespace StickyHomeworks.Models;

public class EchoItem
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("user")]
    public string User { get; set; } = "";
}
