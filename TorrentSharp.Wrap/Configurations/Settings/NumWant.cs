namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>num_want</c> is the number of peers we want from each tracker request. It defines what is sent as the
/// <c>&amp;num_want=</c> parameter to the tracker.
/// </summary>
public sealed record NumWant(int Value) : ISettingsEntry<NumWant>
{
    public static string Key => "num_want";
    public static NumWant FromValue(object value) => new((int)value);
    object ISettingsEntry<NumWant>.Value => Value;
}
