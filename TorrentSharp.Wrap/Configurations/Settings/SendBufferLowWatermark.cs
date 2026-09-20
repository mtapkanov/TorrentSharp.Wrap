namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>send_buffer_low_watermark</c> the minimum send buffer target size (send buffer includes bytes pending being
/// read from disk). For good and snappy seeding performance, set this fairly high, to at least fit a few blocks.
/// This is essentially the initial window size which will determine how fast we can ramp up the send rate
/// </summary>
public sealed record SendBufferLowWatermark(int Value) : ISettingsEntry<SendBufferLowWatermark>
{
    public static string Key => "send_buffer_low_watermark";
    public static SendBufferLowWatermark FromValue(object value) => new((int)value);
    object ISettingsEntry<SendBufferLowWatermark>.Value => Value;
}
