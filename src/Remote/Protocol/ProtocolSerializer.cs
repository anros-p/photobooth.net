using System.Text;
using System.Text.Json;

namespace Photobooth.Remote.Protocol;

/// <summary>
/// Serialises and deserialises protocol messages to/from UTF-8 JSON bytes.
/// </summary>
public static class ProtocolSerializer
{
    private static readonly JsonSerializerOptions Options =
        new(JsonSerializerDefaults.Web) { WriteIndented = false };

    public static byte[] Serialize<T>(T message) =>
        JsonSerializer.SerializeToUtf8Bytes(message, Options);

    public static string SerializeToString<T>(T message) =>
        JsonSerializer.Serialize(message, Options);

    public static T? Deserialize<T>(ReadOnlySpan<byte> bytes) =>
        JsonSerializer.Deserialize<T>(bytes, Options);

    public static T? Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Options);

    /// <summary>Reads the <c>messageType</c> / <c>commandType</c> discriminator without full deserialisation.</summary>
    public static string? ReadType(ReadOnlySpan<byte> bytes)
    {
        try
        {
            var doc = JsonDocument.Parse(bytes.ToArray());
            if (doc.RootElement.TryGetProperty("messageType", out var mt))
                return mt.GetString();
            if (doc.RootElement.TryGetProperty("commandType", out var ct))
                return ct.GetString();
            return null;
        }
        catch { return null; }
    }
}
