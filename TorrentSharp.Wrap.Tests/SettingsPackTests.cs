using System.Net;
using TorrentSharp.Wrap.Configurations;
using TorrentSharp.Wrap.Enums;
using TorrentSharp.Wrap.Utils;
using JetBrains.Annotations;
using TorrentSharp.Wrap.Configurations.Settings;
using TorrentSharp.Wrap.Imports;

namespace TorrentSharp.Wrap.Tests
{
    [TestSubject(typeof(SettingsPack))]
    public class SettingsPackTests : IDisposable
    {
        private readonly TorrentClient _client = new();

        [Fact]
        public void TestSettingsPack()
        {
            var pack = new SettingsPack();
            pack.Set(new UserAgent("libtorrent/1.20"));

            Assert.Equal("libtorrent/1.20", pack.Get<UserAgent>()?.Value);

            _client.UpdateSettings(pack);
        }

        // не соответствует ни одному реальному ключу settings_pack у libtorrent - специально для
        // проверки того, что BuildNative всё ещё падает на нативной валидации ключа.
        private sealed record InvalidData(int Value) : ISettingsEntry<InvalidData>
        {
            public static string Key => "invalid_data";
            public static InvalidData FromValue(object value) => new((int)value);
            object ISettingsEntry<InvalidData>.Value => Value;
        }

        [Fact]
        public void TestInvalidSettings()
        {
            var pack = new SettingsPack();
            pack.Set(new InvalidData(100));

            Assert.Throws<ArgumentException>(() => pack.BuildNative());
        }

        [Fact]
        public void TestListenInterfaces()
        {
            var pack = new SettingsPack();
            ListenInterface[] interfaces =
            [
                new IpInterface(new IPEndPoint(IPAddress.IPv6Any, 6001)),
                new IpInterface(new IPEndPoint(IPAddress.Loopback, 10001), ListenFlags.Ssl)
            ];

            pack.SetListenInterfaces(interfaces);

            Assert.Equal("[::]:6001", interfaces[0].ToString());
            Assert.Equal("127.0.0.1:10001s", interfaces[1].ToString());
            Assert.Contains("[::]:6001", pack.Get<ListenInterfaces>()?.Value);

            // проверяем совместимость с нативным pack'ом
            var nativePack = pack.BuildNative();
            Methods.FreeSettingsPack(nativePack);
        }

        [Fact]
        public void UseListenInterfaces_EmptyList_DoesNotSetKey()
        {
            var pack = new SettingsPack();

            pack.SetListenInterfaces(Array.Empty<ListenInterface>());

            Assert.Null(pack.Get<ListenInterfaces>());
        }

        [Fact]
        public void GuidInterface_ToString_FormatsAsUppercaseBFormatGuid()
        {
            var guid = Guid.Parse("d1e6c9a0-1234-4abc-8def-0123456789ab");

            Assert.Equal("{D1E6C9A0-1234-4ABC-8DEF-0123456789AB}", new GuidInterface(guid).ToString());
            Assert.Equal("{D1E6C9A0-1234-4ABC-8DEF-0123456789AB}s", new GuidInterface(guid, ListenFlags.Ssl).ToString());
        }

        [Fact]
        public void Get_MissingKey_ReturnsNull()
        {
            var pack = new SettingsPack();

            Assert.Null(pack.Get<UserAgent>());
            Assert.Null(pack.Get<ConnectionsLimit>());
            Assert.Null(pack.Get<AnonymousMode>());
        }

        [Fact]
        public void Set_NullValue_ThrowsArgumentNullException()
        {
            var pack = new SettingsPack();

            Assert.Throws<ArgumentNullException>(() => pack.Set<AnonymousMode>(null!));
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}