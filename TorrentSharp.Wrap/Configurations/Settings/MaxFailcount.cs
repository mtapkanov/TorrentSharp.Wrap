namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>max_failcount</c> is the maximum times we try to connect to a peer before stop connecting again. If a peer
/// succeeds, the failure counter is reset. If a peer is retrieved from a peer source (other than DHT) the failcount
/// is decremented by one, allowing another try.
/// </summary>
public sealed record MaxFailcount(int Value) : ISettingsEntry<MaxFailcount>
{
    public static string Key => "max_failcount";
    public static MaxFailcount FromValue(object value) => new((int)value);
    object ISettingsEntry<MaxFailcount>.Value => Value;
}
