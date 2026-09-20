namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>upnp_ignore_nonrouters</c> indicates whether or not the UPnP implementation should ignore any broadcast
/// response from a device whose address is not on our subnet. i.e. it's a way to not talk to other people's routers
/// by mistake.
/// </summary>
public sealed record UpnpIgnoreNonrouters(bool Value) : ISettingsEntry<UpnpIgnoreNonrouters>
{
    public static string Key => "upnp_ignore_nonrouters";
    public static UpnpIgnoreNonrouters FromValue(object value) => new((bool)value);
    object ISettingsEntry<UpnpIgnoreNonrouters>.Value => Value;
}
