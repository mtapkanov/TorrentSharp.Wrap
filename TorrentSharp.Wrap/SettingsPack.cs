using Methods = TorrentSharp.Wrap.Imports.Methods;

namespace TorrentSharp.Wrap;

/// <summary>
/// A key/value store for libtorrent configuration, ready to be applied to a session.
/// </summary>
/// <remarks>
/// See the libtorrent docs for the full list of configuration keys (https://www.libtorrent.org/reference-Settings.html).
/// </remarks>
public class SettingsPack
{
    private readonly Type[] _allowedTypes = [typeof(bool), typeof(int), typeof(string)];
    private readonly Dictionary<string, object> _dictionary = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Reads a configuration value.
    /// </summary>
    /// <param name="key">Key to look up</param>
    /// <typeparam name="T">Expected value type - <see cref="int"/>, <see cref="bool"/>, or <see cref="string"/></typeparam>
    /// <returns>The stored value, or the type's default if the key isn't present</returns>
    /// <exception cref="ArgumentException">T isn't one of the supported types, or doesn't match the stored value's type</exception>
    public T? Get<T>(string key)
    {
        if (!_allowedTypes.Contains(typeof(T)))
            throw new ArgumentException($"Only {string.Join(", ", _allowedTypes.Select(x => x.Name))} are supported");

        if (!_dictionary.TryGetValue(key, out var value))
            return default;

        if (value is T casted)
            return casted;

        throw new ArgumentException($"Type mismatch, expected {value.GetType().Name}, got {typeof(T).Name}");
    }

    public void Set<T>(string key, T value)
    {
        if (!_allowedTypes.Contains(typeof(T)))
            throw new ArgumentException($"Only {string.Join(", ", _allowedTypes.Select(x => x.Name))} are supported");

        if (value is null)
            throw new ArgumentNullException(nameof(value));

        _dictionary[key] = value;
    }

    /// <summary>
    /// Translates the current key/value store into a native settings pack.
    /// </summary>
    /// <returns>Handle to the newly built native pack</returns>
    internal IntPtr BuildNative()
    {
        var settingsPack = Methods.CreateSettingsPack();
        if (settingsPack == IntPtr.Zero)
            throw new ApplicationException("Failed to create settings pack container");

        try
        {
            foreach (var (key, value) in _dictionary)
            {
                var success = value switch
                {
                    int int32 => Methods.SettingsPackSetInt(settingsPack, key, int32),
                    bool boolean => Methods.SettingsPackSetBool(settingsPack, key, boolean),
                    string text => Methods.SettingsPackSetString(settingsPack, key, text),
                    _ => throw new ArgumentException($"{value?.GetType().Name} type is not supported")
                };

                if (!success)
                    throw new ArgumentException($"Failed to set key {key} in settings pack. Ensure the key exists and the value is the correct type.");
            }
        }
        catch
        {
            Methods.FreeSettingsPack(settingsPack);
            throw;
        }

        return settingsPack;
    }
}