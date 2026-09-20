using TorrentSharp.Wrap.Configurations.Settings;
using Methods = TorrentSharp.Wrap.Imports.Methods;

namespace TorrentSharp.Wrap.Configurations;

/// <summary>
/// A key/value store for libtorrent configuration, ready to be applied to a session.
/// </summary>
/// <remarks>
/// See the libtorrent docs for the full list of configuration keys (https://www.libtorrent.org/reference-Settings.html).
/// </remarks>
public class SettingsPack
{
    private readonly Dictionary<string, object> _dictionary = new(StringComparer.OrdinalIgnoreCase);

    public SettingsPack Set<TEntry>(TEntry entry) where TEntry : ISettingsEntry<TEntry>
    {
        ArgumentNullException.ThrowIfNull(entry);

        _dictionary[TEntry.Key] = entry.Value;
        return this;
    }

    /// <summary>
    /// Reads a configuration value as its typed entry.
    /// </summary>
    /// <typeparam name="TEntry">Entry type to look up, e.g. <see cref="AnonymousMode"/></typeparam>
    /// <returns>The stored entry, or <c>null</c> if the key isn't present</returns>
    public TEntry? Get<TEntry>() where TEntry : ISettingsEntry<TEntry>
    {
        return _dictionary.TryGetValue(TEntry.Key, out var value) ? TEntry.FromValue(value) : default;
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

// TSelf - CRTP-ограничение (интерфейс параметризован самим реализующим типом), нужное только
// FromValue: чтобы Get<TEntry> мог собрать обратно правильную запись, зная только TEntry, фабрике
// нужно вернуть именно TSelf, а не ISettingsEntry.
public interface ISettingsEntry<TSelf> where TSelf : ISettingsEntry<TSelf>
{
    // static abstract - в отличие от обычного static с телом, каждый реализующий тип обязан дать
    // свои Key/FromValue, и обратиться к ним можно только через параметр, ограниченный этим
    // интерфейсом (TEntry в Set<TEntry>/Get<TEntry>), а не через сам ISettingsEntry<TSelf>.
    static abstract string Key { get; }

    // распаковывает значение из object, снятого со словаря в Get<TEntry> - симметрично Value ниже.
    static abstract TSelf FromValue(object value);

    // object, а не типизированное значение - Set<TEntry>/Get<TEntry> не параметризованы по типу
    // значения (только по типу записи), так что им нужен единый способ прочитать значение без
    // знания его конкретного типа. Явная реализация интерфейса ниже не мешает записи снаружи
    // выставлять своё Value типизированно (bool/int/string) через обычное позиционное свойство.
    object Value { get; }
}
