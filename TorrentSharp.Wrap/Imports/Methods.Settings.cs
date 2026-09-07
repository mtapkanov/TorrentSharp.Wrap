using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports;

internal static partial class Methods
{
    /// <summary>
    /// Allocates an empty, native settings pack.
    /// </summary>
    /// <remarks>
    /// Populate the result with the SettingsPackSet* methods, then apply it via
    /// <see cref="ApplySettingsPack"/> or <see cref="CreateSession"/>. The handle must be
    /// released manually regardless of whether it was ever applied to a session.
    /// </remarks>
    /// <returns>Handle to the new pack</returns>
    [LibraryImport(LibraryName, EntryPoint = "create_settings_pack")]
    public static partial IntPtr CreateSettingsPack();

    /// <summary>
    /// Releases a settings pack.
    /// </summary>
    /// <param name="settingsPack">Handle to release</param>
    [LibraryImport(LibraryName, EntryPoint = "destroy_settings_pack")]
    public static partial void FreeSettingsPack(IntPtr settingsPack);

    /// <summary>
    /// Writes an <see cref="int"/> value into a settings pack.
    /// </summary>
    /// <param name="settingsPack">Pack to modify</param>
    /// <param name="key">Configuration key to set</param>
    /// <param name="value">Value to assign</param>
    /// <returns>
    /// <c>true</c> if the value was applied; <c>false</c> if the key doesn't exist or expects a different type.
    /// </returns>
    [return: MarshalAs(UnmanagedType.I1)]
    [LibraryImport(LibraryName, EntryPoint = "settings_pack_set_int", StringMarshalling = StringMarshalling.Utf8)]
    public static partial bool SettingsPackSetInt(IntPtr settingsPack, string key, int value);

    /// <summary>
    /// Writes a <see cref="bool"/> value into a settings pack.
    /// </summary>
    /// <param name="settingsPack">Pack to modify</param>
    /// <param name="key">Configuration key to set</param>
    /// <param name="value">Value to assign</param>
    /// <returns>
    /// <c>true</c> if the value was applied; <c>false</c> if the key doesn't exist or expects a different type.
    /// </returns>
    [return: MarshalAs(UnmanagedType.I1)]
    [LibraryImport(LibraryName, EntryPoint = "settings_pack_set_bool", StringMarshalling = StringMarshalling.Utf8)]
    public static partial bool SettingsPackSetBool(IntPtr settingsPack, string key, [MarshalAs(UnmanagedType.I1)] bool value);

    /// <summary>
    /// Writes a <see cref="string"/> value into a settings pack.
    /// </summary>
    /// <param name="settingsPack">Pack to modify</param>
    /// <param name="key">Configuration key to set</param>
    /// <param name="value">Value to assign</param>
    /// <returns>
    /// <c>true</c> if the value was applied; <c>false</c> if the key doesn't exist or expects a different type.
    /// </returns>
    [return: MarshalAs(UnmanagedType.I1)]
    [LibraryImport(LibraryName, EntryPoint = "settings_pack_set_str", StringMarshalling = StringMarshalling.Utf8)]
    public static partial bool SettingsPackSetString(IntPtr settingsPack, string key, string value);
}
