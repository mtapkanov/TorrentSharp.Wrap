namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// when set to true, enables the attempt to use SetFileValidData() to pre-allocate disk space. This system call
/// will only work when running with Administrator privileges on Windows, and so this setting is only relevant in
/// that scenario. Using SetFileValidData() poses a security risk, as it may reveal previously deleted information
/// from the disk.
/// </summary>
public sealed record EnableSetFileValidData(bool Value) : ISettingsEntry<EnableSetFileValidData>
{
    public static string Key => "enable_set_file_valid_data";
    public static EnableSetFileValidData FromValue(object value) => new((bool)value);
    object ISettingsEntry<EnableSetFileValidData>.Value => Value;
}
