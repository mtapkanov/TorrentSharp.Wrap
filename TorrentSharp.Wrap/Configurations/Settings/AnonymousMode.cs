namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>anonymous_mode</c>: When set to true, the client tries to hide its identity to a certain degree.
/// </summary>
public sealed record AnonymousMode(bool Value) : ISettingsEntry<AnonymousMode>
{
    public static string Key => "anonymous_mode";
    public static AnonymousMode FromValue(object value) => new((bool)value);
    object ISettingsEntry<AnonymousMode>.Value => Value;
}
