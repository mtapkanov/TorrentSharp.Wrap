namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>initial_picker_threshold</c> specifies the number of pieces we need before we switch to rarest first picking.
/// The first <c>initial_picker_threshold</c> pieces in any torrent are picked at random , the following pieces are
/// picked in rarest first order.
/// </summary>
public sealed record InitialPickerThreshold(int Value) : ISettingsEntry<InitialPickerThreshold>
{
    public static string Key => "initial_picker_threshold";
    public static InitialPickerThreshold FromValue(object value) => new((int)value);
    object ISettingsEntry<InitialPickerThreshold>.Value => Value;
}
