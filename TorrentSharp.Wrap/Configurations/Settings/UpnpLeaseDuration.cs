namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// The expiration time of UPnP port-mappings, specified in seconds. 0 means permanent lease. Some routers do not
/// support expiration times on port-maps (nor correctly returning an error indicating lack of support). In those
/// cases, set this to 0. Otherwise, don't set it any lower than 5 minutes.
/// </summary>
public sealed record UpnpLeaseDuration(int Value) : ISettingsEntry<UpnpLeaseDuration>
{
    public static string Key => "upnp_lease_duration";
    public static UpnpLeaseDuration FromValue(object value) => new((int)value);
    object ISettingsEntry<UpnpLeaseDuration>.Value => Value;
}
