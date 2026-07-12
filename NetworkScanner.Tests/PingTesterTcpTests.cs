using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NetworkScanner;

namespace NetworkScanner.Tests
{
    public class PingTesterTcpTests
    {
        [Fact]
        public async Task IsAliveByTcpAsync_ReturnsTrue_WhenOneProbePortIsListening()
        {
            // 127.0.0.1에서 임시 포트를 열어두고, 그 포트를 프로브 대상에 포함하면 살아있음으로 판정해야 한다.
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            try
            {
                bool alive = await PingTester.IsAliveByTcpAsync(new[] { 1, 2, port }, "127.0.0.1", timeoutMs: 500);
                Assert.True(alive);
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task IsAliveByTcpAsync_ReturnsFalse_WhenNoProbePortResponds()
        {
            // 닫혀 있을 가능성이 매우 높은 포트들만 두드리면 살아있지 않음으로 판정해야 한다.
            bool alive = await PingTester.IsAliveByTcpAsync(new[] { 1, 2, 3 }, "127.0.0.1", timeoutMs: 300);
            Assert.False(alive);
        }
    }
}
