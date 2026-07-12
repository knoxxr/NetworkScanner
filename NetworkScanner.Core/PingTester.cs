using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkScanner
{
    public class PingTester
    {
        // 플랫폼 종속적인 로깅에 직접 의존하지 않기 위한 선택적 오류 콜백.
        public static Action<string>? OnError { get; set; }

        public static List<int> _PortList = new List<int>();

        // 대역 스캔 중 매 IP마다 같은 권한 오류가 반복 발생해도 안내 메시지는 한 번만 기록한다.
        private static bool _permissionHintLogged;

        public static PingReply? SendPing(IPAddress targetIP)
        {
            return SendPingInternal(targetIP);
        }

        public static PingReply? SendPing(string targetIP)
        {
            return SendPingInternal(IPAddress.Parse(targetIP));
        }

        private const int PingTimeoutMs = 500;
        private static readonly byte[] PingBuffer = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

        private static PingReply? SendPingInternal(IPAddress ip)
        {
            using Ping pingSender = new Ping();
            PingOptions options = new PingOptions { DontFragment = true };

            try
            {
                return pingSender.Send(ip, PingTimeoutMs, PingBuffer, options);
            }
            catch (Exception ex)
            {
                ReportPingFailure(ex);
                return null;
            }
        }

        // 대역 스캔에서 여러 IP를 동시에 검사할 때 스레드를 500ms씩 붙잡지 않도록 하는 비동기 Ping.
        public static async Task<PingReply?> SendPingAsync(IPAddress ip)
        {
            using Ping pingSender = new Ping();
            PingOptions options = new PingOptions { DontFragment = true };

            try
            {
                return await pingSender.SendPingAsync(ip, PingTimeoutMs, PingBuffer, options);
            }
            catch (Exception ex)
            {
                ReportPingFailure(ex);
                return null;
            }
        }

        public static async Task<string> CheckPortsOpenAsync(string ip)
        {
            var result = new StringBuilder();
            foreach (int port in _PortList)
            {
                if (await CheckPortAsync(ip, port))
                {
                    result.Append(port).Append('/');
                }
            }
            return result.ToString();
        }

        // ICMP Ping에 응답하지 않지만 살아있는 호스트(방화벽이 ping을 막는 서버/프린터/IoT 등)를 놓치지 않도록,
        // 대표 포트 몇 개에 TCP 연결을 시도해 하나라도 열려 있으면 살아있는 것으로 본다. 열린 포트가 나오는
        // 즉시 반환하고, 대상 호스트가 죽어 있으면 timeoutMs 안에 모두 실패하며 끝난다.
        public static async Task<bool> IsAliveByTcpAsync(int[] ports, string ip, int timeoutMs = 400)
        {
            var pending = new List<Task<bool>>();
            foreach (int port in ports)
            {
                pending.Add(CheckPortAsync(ip, port, timeoutMs));
            }

            while (pending.Count > 0)
            {
                Task<bool> finished = await Task.WhenAny(pending);
                pending.Remove(finished);
                if (await finished) return true;
            }
            return false;
        }

        public static async Task<bool> CheckPortAsync(string ip, int port, int timeoutMs = 300)
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                using var cts = new CancellationTokenSource(timeoutMs);
                await socket.ConnectAsync(ip, port, cts.Token);
                return socket.Connected;
            }
            catch
            {
                // 연결 거부/타임아웃/취소 등은 모두 "닫힘"으로 간주한다.
                return false;
            }
        }

        private static void ReportPingFailure(Exception ex)
        {
            if (IsPermissionDenied(ex) && !_permissionHintLogged)
            {
                _permissionHintLogged = true;
                OnError?.Invoke(
                    "ICMP Ping 권한이 없어 이후 스캔 결과가 부정확할 수 있습니다. " +
                    "Linux에서는 'sudo setcap cap_net_raw+ep <실행파일 경로>' 실행 후 다시 시도하거나 관리자/루트 권한으로 실행하세요. " +
                    "(원본 오류: " + ex.Message + ")");
                return;
            }

            OnError?.Invoke(ex.Message);
        }

        private static bool IsPermissionDenied(Exception? ex)
        {
            if (ex == null) return false;

            if (ex is SocketException se &&
                (se.SocketErrorCode == SocketError.AccessDenied || (int)se.SocketErrorCode == 13))
            {
                return true;
            }

            if (IsPermissionDenied(ex.InnerException)) return true;

            return ex.Message.Contains("access", StringComparison.OrdinalIgnoreCase)
                && ex.Message.Contains("denied", StringComparison.OrdinalIgnoreCase);
        }

        public static string CheckPortsOpen(string ip)
        {
            string result="";
            foreach(int port in _PortList)
            {
                if(CheckPort(ip, port))
                {
                    result += port + "/";
                }
            }

            return result;
        }

        public static bool CheckReservedPortsOpen(string ip, int port)
        {
            return CheckPort(ip, port);
        }

        public static bool CheckProhibitPortsOpen(string ip, int port)
        {
            return CheckPort(ip, port);
        }


        public static bool CheckPort(string ip, int port)
        {
            bool result = false;
            Socket socket = null;
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, false);
                IAsyncResult ret = socket.BeginConnect(ip, port, null, null);
                result = ret.AsyncWaitHandle.WaitOne(100, true);
            }
            catch
            {

            }
            finally
            {
                if (socket != null)
                {
                    socket.Close();
                }
            }
            return result;
        }

    }
}
