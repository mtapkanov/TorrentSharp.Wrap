namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// for some aio back-ends, <c>aio_threads</c> specifies the number of io-threads to use.
/// </summary>
public sealed record AioThreads(int Value) : ISettingsEntry<AioThreads>
{
    public static string Key => "aio_threads";
    public static AioThreads FromValue(object value) => new((int)value);
    object ISettingsEntry<AioThreads>.Value => Value;
}
