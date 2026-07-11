using System.Collections.Generic;
using System.Net;

namespace NetworkScanner
{
    // UCIPList가 스캔에 필요한 설정값(포트 목록, FTP 계정, 시스템명 등)을 얻기 위해 의존하는 추상화.
    // MainNetworkScanner를 직접 캐스팅하는 대신 이 인터페이스에 의존함으로써 UCIPList를
    // 특정 Window 구현으로부터 분리하고, 테스트/재사용 시 다른 구현으로 교체할 수 있게 한다.
    public interface IScanConfigProvider
    {
        string GetPortList();
        IPAddress GetFTPIP();
        string GetFTPID();
        string GetFTPPW();
        int GetFTPPort();
        bool? GetUseFTP();
        string GetSystemName();
        bool? GetUsePortChecking();
        List<RefPortInfo> GetReservedPortList();
        List<RefPortInfo> GetProhibitPortList();
        ScanRangeList GetScanRanges();
    }
}
