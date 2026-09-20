namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// if a peer is uninteresting and uninterested for longer than this number of seconds, it will be disconnected.
/// </summary>
public sealed record InactivityTimeout(int Value) : ISettingsEntry<InactivityTimeout>
{
    public static string Key => "inactivity_timeout";
    public static InactivityTimeout FromValue(object value) => new((int)value);
    object ISettingsEntry<InactivityTimeout>.Value => Value;
}
