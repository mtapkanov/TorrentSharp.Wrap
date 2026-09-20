using TorrentSharp.Wrap.Configurations;
using TorrentSharp.Wrap.Configurations.Settings;

namespace TorrentSharp.Wrap.Utils;

public static class SettingsPackExtensions
{
    /// <summary>
    /// Serializes a set of listen interfaces into the <c>listen_interfaces</c> key of a <see cref="SettingsPack"/>.
    /// </summary>
    public static SettingsPack SetListenInterfaces(this SettingsPack pack, params ListenInterface[] interfaces)
    {
        if (string.Join(',', interfaces) is { Length: > 0 } value)
            pack.Set(new ListenInterfaces(value));

        return pack;
    }
}