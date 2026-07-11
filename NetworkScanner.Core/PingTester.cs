using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

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

        private static PingReply? SendPingInternal(IPAddress ip)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

            byte[] buffer = Encoding.ASCII.GetBytes(data);

            int timeout = 500;

            try
            {
                return pingSender.Send(ip, timeout, buffer, options);
            }
            catch (Exception ex)
            {
                ReportPingFailure(ex);
                return null;
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
