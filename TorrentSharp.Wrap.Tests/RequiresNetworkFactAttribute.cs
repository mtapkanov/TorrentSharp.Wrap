namespace TorrentSharp.Wrap.Tests;

// Как [Fact], но реально выполняется только если включён хотя бы один из двух флагов - таким
// тестам нужен настоящий исходящий BitTorrent-трафик (DHT/трекеры/пиры по сырому TCP/UDP),
// который доступен не в любом окружении (например, в песочницах с исходящим доступом только по
// HTTPS). По умолчанию оба флага выключены, чтобы обычный `dotnet test` оставался быстрым и не
// висел на таймаутах:
//   - конструкторный параметр enabled - переключается прямо в атрибуте на конкретном тесте;
//   - переменная окружения WITH_NETWORK_TESTS=1 - включает сразу все такие тесты извне,
//     без правки кода (например, в CI с реальной сетью).
[AttributeUsage(AttributeTargets.Method)]
public sealed class RequiresNetworkFactAttribute : FactAttribute
{
    public RequiresNetworkFactAttribute(bool enabled)
    {
        if (!enabled && Environment.GetEnvironmentVariable("WITH_NETWORK_TESTS") != "1")
        {
            Skip = "Requires real network access to DHT/trackers/peers. Enable it either by running " +
                   "\"WITH_NETWORK_TESTS=1 dotnet test\", or by passing enabled: true to this attribute on the test.";
        }
    }
}
