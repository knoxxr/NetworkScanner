using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkScanner
{
    public class PingTester
    {
        // 플랫폼 종속적인 로깅에 직접 의존하지 않기 위한 선택적 오류 콜백.
        public static Action<string>? OnError { get; set; }

        public static List<int> _PortList = new List<int>();

        public static PingReply SendPing(IPAddress targetIP)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

            byte[] buffer = Encoding.ASCII.GetBytes(data);

            int timeout = 500;

            PingReply reply = pingSender.Send(targetIP, timeout, buffer, options);

            return reply;
        }

        public static PingReply SendPing(string targetIP)
        {
            IPAddress ip = IPAddress.Parse(targetIP);
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

            byte[] buffer = Encoding.ASCII.GetBytes(data);

            int timeout = 500;
            PingReply reply;
            try
            {
                reply = pingSender.Send(ip, timeout, buffer, options);
            }
            catch (Exception e)
            {
                OnError?.Invoke(e.Message);
                return null;
            }

            return reply;
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
