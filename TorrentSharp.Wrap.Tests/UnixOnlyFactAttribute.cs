namespace TorrentSharp.Wrap.Tests;

/// <summary>
/// A <see cref="FactAttribute"/> that reports itself as skipped (with a reason, visible in the test
/// report) on Windows instead of running - for tests that rely on Unix-only APIs
/// (<c>File.SetUnixFileMode</c>, ...). This library isn't built for Windows anyway (see
/// scripts/build.sh's presets), so there's nothing to exercise there; a real xUnit v2 Skip has to be
/// set before discovery, which is why this is a constructor check rather than a runtime guard inside
/// the test body - that would just make the test pass trivially instead of showing as skipped.
/// </summary>
public sealed class UnixOnlyFactAttribute : FactAttribute
{
    public UnixOnlyFactAttribute()
    {
        if (OperatingSystem.IsWindows())
            Skip = "Unix-only test (uses File.SetUnixFileMode) - this library isn't built for Windows.";
    }
}
