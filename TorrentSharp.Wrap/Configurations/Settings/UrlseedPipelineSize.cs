namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// controls the pipelining size of url and http seeds. i.e. the number of HTTP request to keep outstanding before
/// waiting for the first one to complete. It's common for web servers to limit this to a relatively low number,
/// like 5
/// </summary>
public sealed record UrlseedPipelineSize(int Value) : ISettingsEntry<UrlseedPipelineSize>
{
    public static string Key => "urlseed_pipeline_size";
    public static UrlseedPipelineSize FromValue(object value) => new((int)value);
    object ISettingsEntry<UrlseedPipelineSize>.Value => Value;
}
