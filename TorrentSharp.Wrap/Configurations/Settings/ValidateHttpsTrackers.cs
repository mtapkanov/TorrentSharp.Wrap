namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// when set to true, the certificate of HTTPS trackers and HTTPS web seeds will be validated against the system's
/// certificate store (as defined by OpenSSL). If the system does not have a certificate store, this option may have
/// to be disabled in order to get trackers and web seeds to work).
/// </summary>
public sealed record ValidateHttpsTrackers(bool Value) : ISettingsEntry<ValidateHttpsTrackers>
{
    public static string Key => "validate_https_trackers";
    public static ValidateHttpsTrackers FromValue(object value) => new((bool)value);
    object ISettingsEntry<ValidateHttpsTrackers>.Value => Value;
}
