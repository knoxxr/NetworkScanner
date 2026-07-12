using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NetworkScanner
{
    public class IPInfoList : ObservableCollection<IPInfo>
    {
        public IPInfoList()
        {

        }

        public void AddItem(IPInfo newitem)
        {
            if(!IsExist(newitem.Ip))
            {
                this.Add(newitem);
            }
        }

        public IPInfo GetItem(string ip)
        {
            return this.Items.FirstOrDefault(x => x.Ip == ip);
        }

        public void DelItem(string ip)
        {
            if (IsExist(ip))
            {
                this.Remove(this.Where(x => x.Ip == ip).FirstOrDefault());
            }

        }

        public bool IsExist(string ip)
        {
            if( this.Where(x=>x.Ip==ip).FirstOrDefault() == null)
                return false;
            else
                return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            if (!String.IsNullOrEmpty(name))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        public string GetHostName(IPAddress hostip)
        {
            string result = "";
            try
            {
                IPHostEntry host = Dns.GetHostByAddress(hostip);
                result = host.HostName;
            }
            catch (System.Net.Sockets.SocketException)
            {
                return result;
            }
            return result;
        }
        public string? GetMACAddress(string ip)
        {
            return ArpResolver.GetMacAddress(ip);
        }

    }

    public class IPInfo
    {
        private string ip;
        private string ports;
        private string systemName;
        private string roundTime;
        private bool alive;
        private string commitDate;
        private string description;
        private string macaddr;
        private string vendor;
        private bool hasProhibitedPort;

        public string Ip
        {
            get => ip;
            set
            {
                ip = value;
            }
        }
        public string Ports { get => ports; set => ports = value; }
        public string SystemName { get => systemName; set => systemName = value; }
        public string RountTime { get => roundTime; set => roundTime = value; }
        public bool Alive { get => alive; set => alive = value; }
        public string CommitDate { get => commitDate; set => commitDate = value; }
        public string Description { get => description; set => description = value; }
        public string Macaddr { get => macaddr; set => macaddr = value; }
        public string Vendor { get => vendor; set => vendor = value; }

        // 스캔 시 열린 포트 중 위험(백도어) 포트 목록과 일치하는 것이 있으면 true.
        public bool HasProhibitedPort { get => hasProhibitedPort; set => hasProhibitedPort = value; }

        // UI가 상태를 색상/배지로 표시할 때 쓰는 3단계 상태 키. Alive/HasProhibitedPort 조합으로 계산되므로
        // 별도 저장·동기화가 필요 없다.
        public string StatusKey => !Alive ? "bad" : (HasProhibitedPort ? "warn" : "good");
        public string StatusText => !Alive ? "없음" : (HasProhibitedPort ? "주의" : "정상");

        // 제조사·열린 포트로 추정한 장비 종류(참고용). UI의 "종류" 컬럼에 표시된다.
        public string DeviceType => DeviceClassifier.Classify(vendor, ports);
    }
}
