using System.Runtime.InteropServices;
using TorrentSharp.Wrap.Imports.Events;
using TorrentSharp.Wrap.Imports.Structs;

namespace TorrentSharp.Wrap.Imports;

internal static partial class Methods
{
    /// <summary>
    /// Callback signature invoked for events raised by a session.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SessionEventCallback(IntPtr alertPtr);

    /// <summary>
    /// Creates a libtorrent session from a native settings pack, optionally starting its DHT from a state
    /// returned by <see cref="GetSessionState"/>.
    /// </summary>
    /// <param name="settingsPack">Native settings pack handle</param>
    /// <param name="state">Previous session state, or <c>null</c></param>
    /// <param name="stateLength">Length of <paramref name="state"/></param>
    [LibraryImport(LibraryName, EntryPoint = "create_session")]
    public static unsafe partial IntPtr CreateSession(void* settingsPack, byte[]? state, long stateLength);

    /// <summary>
    /// Serializes the session's DHT state into a native-allocated buffer, released with <see cref="FreeTorrentBytes"/>.
    /// </summary>
    [return: MarshalAs(UnmanagedType.I1)]
    [LibraryImport(LibraryName, EntryPoint = "get_session_state")]
    public static partial bool GetSessionState(IntPtr sessionHandle, out IntPtr data, out long size);

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

    /// <summary>
    /// Reads whether the session's DHT node is currently running.
    /// </summary>
    /// <param name="sessionHandle">Session to query</param>
    [return: MarshalAs(UnmanagedType.I1)]
    [LibraryImport(LibraryName, EntryPoint = "is_session_dht_running")]
    public static partial bool IsSessionDhtRunning(IntPtr sessionHandle);

    /// <summary>
    /// Adds a bootstrap node to the session's DHT routing table.
    /// </summary>
    /// <param name="sessionHandle">Session to add the node to</param>
    /// <param name="host">Hostname or IP address of the node</param>
    /// <param name="port">Port the node listens on</param>
    [LibraryImport(LibraryName, EntryPoint = "add_session_dht_node", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void AddSessionDhtNode(IntPtr sessionHandle, [MarshalAs(UnmanagedType.LPUTF8Str)] string host, int port);

    /// <summary>
    /// Replaces the session's IP filter with these rules; an empty set clears it.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "set_session_ip_filter", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void SetSessionIpFilter(IntPtr sessionHandle, string[] firstIps, string[] lastIps, byte[] blocked, int count);

    /// <summary>
    /// Reads the process-constant name/index table describing every metric a session stats
    /// notification's counters array can carry.
    /// </summary>
    /// <param name="metrics">Populated with the metric table on return</param>
    [LibraryImport(LibraryName, EntryPoint = "get_session_stats_metrics")]
    public static partial void GetSessionStatsMetrics(out SessionStatsMetricList metrics);

    /// <summary>
    /// Releases a metric table previously returned by <see cref="GetSessionStatsMetrics"/>.
    /// </summary>
    /// <param name="metrics">Table to release</param>
    [LibraryImport(LibraryName, EntryPoint = "destroy_session_stats_metric_list")]
    public static partial void FreeSessionStatsMetricList(ref SessionStatsMetricList metrics);

    /// <summary>
    /// Triggers an asynchronous sample of the session's counters/gauges. Completion is reported
    /// via a <see cref="Enums.NotificationType.SessionStats"/> notification.
    /// </summary>
    /// <param name="sessionHandle">Session to sample</param>
    [LibraryImport(LibraryName, EntryPoint = "post_session_stats")]
    public static partial void PostSessionStats(IntPtr sessionHandle);
}
