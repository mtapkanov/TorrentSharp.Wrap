namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// specifies the buffer sizes set on peer sockets. 0 means the OS default (i.e. don't change the buffer sizes). The
/// socket buffer sizes are changed using setsockopt() with SOL_SOCKET/SO_RCVBUF and SO_SNDBUFFER.
/// </summary>
public sealed record RecvSocketBufferSize(int Value) : ISettingsEntry<RecvSocketBufferSize>
{
    public static string Key => "recv_socket_buffer_size";
    public static RecvSocketBufferSize FromValue(object value) => new((int)value);
    object ISettingsEntry<RecvSocketBufferSize>.Value => Value;
}
