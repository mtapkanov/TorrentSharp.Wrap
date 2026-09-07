using System.Runtime.InteropServices;
using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Imports;

internal static partial class Methods
{
    /// <summary>
    /// Callback signature invoked for events raised by a session.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SessionEventCallback(IntPtr alertPtr);

    /// <summary>
    /// Creates a session, optionally seeded from an existing settings pack.
    /// </summary>
    /// <param name="settingsPack">Settings pack handle, or <c>null</c> to use the defaults</param>
    /// <returns>Handle to the new session</returns>
    [LibraryImport(LibraryName, EntryPoint = "create_session")]
    public static unsafe partial IntPtr CreateSession(void* settingsPack);

    /// <summary>
    /// Releases the unmanaged resources held by a session.
    /// </summary>
    /// <param name="sessionHandle">Session to tear down</param>
    [LibraryImport(LibraryName, EntryPoint = "destroy_session")]
    public static partial void FreeSession(IntPtr sessionHandle);

    /// <summary>
    /// Registers the event callback for a session.
    /// </summary>
    /// <param name="sessionHandle">Handle of the session to attach the callback to</param>
    /// <param name="callback">Invoked whenever an event is posted</param>
    /// <param name="includeUnmappedEvents">Whether <see cref="callback"/> should also run for event types with no dedicated struct, which only carry an <see cref="EventBase"/> payload</param>
    [LibraryImport(LibraryName, EntryPoint = "set_event_callback")]
    public static partial void SetEventCallback(IntPtr sessionHandle, [MarshalAs(UnmanagedType.FunctionPtr)] SessionEventCallback callback, [MarshalAs(UnmanagedType.Bool)] bool includeUnmappedEvents);

    /// <summary>
    /// Unregisters whatever event callback is currently set.
    /// </summary>
    /// <param name="sessionHandle">Handle of the session to detach the callback from</param>
    [LibraryImport(LibraryName, EntryPoint = "clear_event_callback")]
    public static partial void ClearEventCallback(IntPtr sessionHandle);

    /// <summary>
    /// Applies a settings pack to an existing session.
    /// </summary>
    /// <param name="sessionHandle">Handle of the session to reconfigure</param>
    /// <param name="settingsPack">Handle of the pack to apply</param>
    [LibraryImport(LibraryName, EntryPoint = "apply_settings")]
    public static partial void ApplySettingsPack(IntPtr sessionHandle, IntPtr settingsPack);
}
