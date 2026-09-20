namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>suggest_mode</c> controls whether or not libtorrent will send out suggest messages to create a bias of its
/// peers to request certain pieces. The modes are:
/// </summary>
public sealed record SuggestMode(int Value) : ISettingsEntry<SuggestMode>
{
    public static string Key => "suggest_mode";
    public static SuggestMode FromValue(object value) => new((int)value);
    object ISettingsEntry<SuggestMode>.Value => Value;
}
