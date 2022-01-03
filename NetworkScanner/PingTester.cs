using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace NetworkScanner
{
    public class PingTester
    {
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

            PingReply reply = pingSender.Send(ip, timeout, buffer, options);

            return reply;
        }
    }
}
