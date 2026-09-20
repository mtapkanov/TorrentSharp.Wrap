namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>share_mode_target</c> specifies the target share ratio for share mode torrents. If set to 3, we'll try to
/// upload 3 times as much as we download. Setting this very high, will make it very conservative and you might end
/// up not downloading anything ever (and not affecting your share ratio). It does not make any sense to set this
/// any lower than 2. For instance, if only 3 peers need to download the rarest piece, it's impossible to download a
/// single piece and upload it more than 3 times. If the share_mode_target is set to more than 3, nothing is
/// downloaded.
/// </summary>
public sealed record ShareModeTarget(int Value) : ISettingsEntry<ShareModeTarget>
{
    public static string Key => "share_mode_target";
    public static ShareModeTarget FromValue(object value) => new((int)value);
    object ISettingsEntry<ShareModeTarget>.Value => Value;
}
