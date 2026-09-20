namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the max number of bytes to allow an HTTP response to be when announcing to trackers or downloading .torrent
/// files via the <c>url</c> provided in <c>add_torrent_params</c>.
/// </summary>
public sealed record MaxHttpRecvBufferSize(int Value) : ISettingsEntry<MaxHttpRecvBufferSize>
{
    public static string Key => "max_http_recv_buffer_size";
    public static MaxHttpRecvBufferSize FromValue(object value) => new((int)value);
    object ISettingsEntry<MaxHttpRecvBufferSize>.Value => Value;
}
