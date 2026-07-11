using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;

namespace NetworkScanner
{
    public class ScanRangeInfo
    {
        public int Index { get; set; }
        public string StartIP { get; set; }
        public string EndIP { get; set; }
        public string Description { get; set; }
    }

    public class ScanRangeList : List<ScanRangeInfo>
    {
        public ScanRangeList()
        {
        }

        // 실제로 추가되었으면 true, 이미 등록된(시작/종료 IP가 모두 같은) 대역이라 건너뛰었으면 false.
        public bool AddItem(ScanRangeInfo item)
        {
            if (IsExist(item.StartIP, item.EndIP))
            {
                return false;
            }

            this.Add(item);
            return true;
        }

        public void DelItem(string startIp, string endIp)
        {
            if (IsExist(startIp, endIp))
            {
                this.Remove(this.Where(x => x.StartIP == startIp && x.EndIP == endIp).FirstOrDefault());
            }
        }

        public bool IsExist(int index)
        {
            if (this.Where(x => x.Index == index).FirstOrDefault() == null)
                return false;
            else
                return true;
        }

        // 시작/종료 IP가 둘 다 같은 대역이 이미 있는지 확인한다(둘 중 하나만 같은 건 다른 대역이다).
        public bool IsExist(string startIp, string endIp)
        {
            return this.Any(x => x.StartIP == startIp && x.EndIP == endIp);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            if (!String.IsNullOrEmpty(name))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public static class IPRangeUtil
    {
        // IPv4 주소를 정렬/증감 가능한 정수로 변환한다. 마지막 옥텟만으로 범위를 다루면
        // 시작/종료 IP가 서로 다른 /24 대역에 걸칠 때(예: 10.0.1.250~10.0.2.10) 스캔이 깨지므로
        // 전체 32bit 값 기준으로 IP 범위를 계산한다.
        public static uint ToUInt32(IPAddress ip)
        {
            byte[] bytes = ip.GetAddressBytes();
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public static IPAddress FromUInt32(uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return new IPAddress(bytes);
        }
    }
}
