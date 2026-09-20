namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the number of seconds before the internal host name resolver considers a cache value timed out, negative values
/// are interpreted as zero.
/// </summary>
public sealed record ResolverCacheTimeout(int Value) : ISettingsEntry<ResolverCacheTimeout>
{
    public static string Key => "resolver_cache_timeout";
    public static ResolverCacheTimeout FromValue(object value) => new((int)value);
    object ISettingsEntry<ResolverCacheTimeout>.Value => Value;
}
