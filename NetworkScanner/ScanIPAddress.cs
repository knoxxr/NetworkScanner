using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NetworkScanner
{
    public class ScanIPAddress : IPAddress
    {
        public ScanIPAddress(byte[] address) : base(address)
        {
        }

        public ScanIPAddress(byte[] address, long scopeid) : base(address)
        {
        }

        public ScanIPAddress(long newAddress) : base(newAddress)
        {
        }

        public ScanIPAddress(ReadOnlySpan<byte> address) : base(address)
        {
        }

        public ScanIPAddress(ReadOnlySpan<byte> address, long scopeid) : base(address)
        {
        }

        public string StrIP
        { 
            get
            {
                return ToString();
            } 
        }
    }
}
