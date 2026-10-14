namespace TorrentSharp.Wrap.Configurations.Options;

/// <summary>How long the async operations wait for libtorrent's answer.</summary>
public sealed class TimeoutOptions
{
    private readonly TorrentClientOptionsBuilder _builder;

    internal TimeoutOptions(TorrentClientOptionsBuilder builder) => _builder = builder;

    /// <summary>
    /// The wait of every async operation called without its own <c>timeout</c> (saving resume data, reading a
    /// piece, scraping, ...). 30 seconds by default.
    /// </summary>
    public TimeoutOptions Default(TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        _builder.DefaultTimeout = timeout;
        return this;
    }
}
