namespace TorrentSharp.Wrap.Configurations.Options;

/// <summary>Where torrents are saved.</summary>
public sealed class StorageOptions
{
    private readonly TorrentClientOptionsBuilder _builder;

    internal StorageOptions(TorrentClientOptionsBuilder builder) => _builder = builder;

    /// <summary>
    /// The directory a torrent attached without a save path goes to, and a relative save path is resolved
    /// against. By default <c>downloads</c> under the process's current directory.
    /// </summary>
    public StorageOptions DefaultDownloadPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _builder.DefaultDownloadPath = path;
        return this;
    }
}
