namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// specify the not-sent low watermark for socket send buffers. This corresponds to the, Linux-specific,
/// <c>TCP_NOTSENT_LOWAT</c> TCP socket option.
/// </summary>
public sealed record SendNotSentLowWatermark(int Value) : ISettingsEntry<SendNotSentLowWatermark>
{
    public static string Key => "send_not_sent_low_watermark";
    public static SendNotSentLowWatermark FromValue(object value) => new((int)value);
    object ISettingsEntry<SendNotSentLowWatermark>.Value => Value;
}
