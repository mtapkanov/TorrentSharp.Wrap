namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// time to wait until a new retry of a web seed name lookup
/// </summary>
public sealed record WebSeedNameLookupRetry(int Value) : ISettingsEntry<WebSeedNameLookupRetry>
{
    public static string Key => "web_seed_name_lookup_retry";
    public static WebSeedNameLookupRetry FromValue(object value) => new((int)value);
    object ISettingsEntry<WebSeedNameLookupRetry>.Value => Value;
}
