namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>connections_slack</c> is the number of incoming connections exceeding the connection limit to accept in order
/// to potentially replace existing ones.
/// </summary>
public sealed record ConnectionsSlack(int Value) : ISettingsEntry<ConnectionsSlack>
{
    public static string Key => "connections_slack";
    public static ConnectionsSlack FromValue(object value) => new((int)value);
    object ISettingsEntry<ConnectionsSlack>.Value => Value;
}
