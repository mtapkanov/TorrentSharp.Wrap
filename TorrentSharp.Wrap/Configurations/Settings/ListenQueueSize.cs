namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>listen_queue_size</c> is the value passed in to listen() for the listen socket. It is the number of
/// outstanding incoming connections to queue up while we're not actively waiting for a connection to be accepted. 5
/// should be sufficient for any normal client. If this is a high performance server which expects to receive a lot
/// of connections, or used in a simulator or test, it might make sense to raise this number. It will not take
/// affect until the <c>listen_interfaces</c> settings is updated.
/// </summary>
public sealed record ListenQueueSize(int Value) : ISettingsEntry<ListenQueueSize>
{
    public static string Key => "listen_queue_size";
    public static ListenQueueSize FromValue(object value) => new((int)value);
    object ISettingsEntry<ListenQueueSize>.Value => Value;
}
