using System.Collections.Generic;
using System.Net;
using NetworkScanner;

namespace NetworkScanner.Tests.Fixtures
{
    public class FakeScanConfigProvider : IScanConfigProvider
    {
        public ScanRangeList ScanRanges { get; set; } = new ScanRangeList();
        public List<RefPortInfo> ReservedPortList { get; set; } = new List<RefPortInfo>();
        public List<RefPortInfo> ProhibitPortList { get; set; } = new List<RefPortInfo>();

        public string GetPortList() => "";
        public IPAddress GetFTPIP() => IPAddress.None;
        public string GetFTPID() => "";
        public string GetFTPPW() => "";
        public int GetFTPPort() => 0;
        public bool? GetUseFTP() => false;
        public string GetSystemName() => "TestSystem";
        public bool? GetUsePortChecking() => false;
        public List<RefPortInfo> GetReservedPortList() => ReservedPortList;
        public List<RefPortInfo> GetProhibitPortList() => ProhibitPortList;
        public ScanRangeList GetScanRanges() => ScanRanges;
    }
}
