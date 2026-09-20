namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>no_recheck_incomplete_resume</c> determines if the storage should check the whole files when resume data is
/// incomplete or missing or whether it should simply assume we don't have any of the data. If false, any existing
/// files will be checked. By setting this setting to true, the files won't be checked, but will go straight to
/// download mode.
/// </summary>
public sealed record NoRecheckIncompleteResume(bool Value) : ISettingsEntry<NoRecheckIncompleteResume>
{
    public static string Key => "no_recheck_incomplete_resume";
    public static NoRecheckIncompleteResume FromValue(object value) => new((bool)value);
    object ISettingsEntry<NoRecheckIncompleteResume>.Value => Value;
}
