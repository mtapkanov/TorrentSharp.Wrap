namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>max_metadata_size</c> is the maximum allowed size (in bytes) to be received by the metadata extension, i.e.
/// magnet links.
/// </summary>
public sealed record MaxMetadataSize(int Value) : ISettingsEntry<MaxMetadataSize>
{
    public static string Key => "max_metadata_size";
    public static MaxMetadataSize FromValue(object value) => new((int)value);
    object ISettingsEntry<MaxMetadataSize>.Value => Value;
}
