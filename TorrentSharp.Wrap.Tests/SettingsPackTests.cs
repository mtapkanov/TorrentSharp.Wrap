using System.Net;
using TorrentSharp.Wrap.Enums;
using TorrentSharp.Wrap.Utils;
using JetBrains.Annotations;
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
            pack.Set("user_agent", "libtorrent/1.20");

            Assert.Equal("libtorrent/1.20", pack.Get<string>("user_agent"));

            _client.UpdateSettings(pack);
        }

        [Fact]
        public void TestInvalidSettings()
        {
            var pack = new SettingsPack();
            pack.Set("invalid_data", 100);

            Assert.Throws<ArgumentException>(() => pack.Get<bool>("invalid_data"));
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
            Assert.Contains("[::]:6001", pack.Get<string>("listen_interfaces"));

            // проверяем совместимость с нативным pack'ом
            var nativePack = pack.BuildNative();
            Methods.FreeSettingsPack(nativePack);
        }

        [Fact]
        public void UseListenInterfaces_EmptyList_DoesNotSetKey()
        {
            var pack = new SettingsPack();

            pack.SetListenInterfaces(Array.Empty<ListenInterface>());

            Assert.Null(pack.Get<string>("listen_interfaces"));
        }

        [Fact]
        public void GuidInterface_ToString_FormatsAsUppercaseBFormatGuid()
        {
            var guid = Guid.Parse("d1e6c9a0-1234-4abc-8def-0123456789ab");

            Assert.Equal("{D1E6C9A0-1234-4ABC-8DEF-0123456789AB}", new GuidInterface(guid).ToString());
            Assert.Equal("{D1E6C9A0-1234-4ABC-8DEF-0123456789AB}s", new GuidInterface(guid, ListenFlags.Ssl).ToString());
        }

        [Fact]
        public void Get_MissingKey_ReturnsDefault()
        {
            var pack = new SettingsPack();

            Assert.Null(pack.Get<string>("does_not_exist"));
            Assert.Equal(0, pack.Get<int>("does_not_exist"));
            Assert.False(pack.Get<bool>("does_not_exist"));
        }

        [Fact]
        public void Set_UnsupportedType_ThrowsArgumentException()
        {
            var pack = new SettingsPack();

            Assert.Throws<ArgumentException>(() => pack.Set("key", 1.5));
        }

        [Fact]
        public void Set_NullValue_ThrowsArgumentNullException()
        {
            var pack = new SettingsPack();

            Assert.Throws<ArgumentNullException>(() => pack.Set<string>("key", null!));
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}