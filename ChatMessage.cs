using System.Text.Json.Serialization;

namespace YMH219_ChatApp;

public sealed record ChatMessage(string Type, string User, string Text, DateTime Timestamp)
{
    public static ChatMessage Create(string type, string user, string text) => new(type, user, text, DateTime.Now);
    public string ToDisplay() => $"{Timestamp:HH:mm:ss}  {User}: {Text}";
}
