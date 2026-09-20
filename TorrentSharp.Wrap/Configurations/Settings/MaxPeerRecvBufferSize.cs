namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the max number of bytes a single peer connection's receive buffer is allowed to grow to.
/// </summary>
public sealed record MaxPeerRecvBufferSize(int Value) : ISettingsEntry<MaxPeerRecvBufferSize>
{
    public static string Key => "max_peer_recv_buffer_size";
    public static MaxPeerRecvBufferSize FromValue(object value) => new((int)value);
    object ISettingsEntry<MaxPeerRecvBufferSize>.Value => Value;
}
