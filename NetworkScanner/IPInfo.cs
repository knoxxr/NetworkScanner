using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NetworkScanner
{
    public class IPInformationListViewModel 
    {
        private readonly IPInfoList items;

        public IPInformationListViewModel()
        {
            this.items = new IPInfoList();
        }

        public IPInfoList Items
        {
            get { return this.items; }
        }
    }

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

        public string GetHostName(string hostip)
        {
            string result = "";
            try
            {
                IPHostEntry host = Dns.GetHostByAddress(IPAddress.Parse(hostip));
                result = host.HostName;
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                return result;
            }
            return result;
        }

        public string GetHostName(IPAddress hostip)
        {
            string result = "";
            try
            {
                IPHostEntry host = Dns.GetHostByAddress(hostip);
                result = host.HostName;
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                return result;
            }
            return result;
        }
        [DllImport("iphlpapi.dll", ExactSpelling = true)] private static extern int SendARP(int destinationIPValue, int sourceIPValue, byte[] physicalAddressArray, ref uint physicalAddresArrayLength);
        public string GetMACAddress(string ip)
        {
            IPAddress destinationIPAddress = IPAddress.Parse(ip);
            byte[] destinationIPAddressByteArray = new byte[6];
            uint destinationIPAddressByteArrayLength = (uint)destinationIPAddressByteArray.Length;
            int destinationIPValue = BitConverter.ToInt32(destinationIPAddress.GetAddressBytes(), 0);
            int returnCode;

            try
            {
                returnCode = SendARP(destinationIPValue, 0, destinationIPAddressByteArray, ref destinationIPAddressByteArrayLength);
            }
            catch (Exception ex)
            {
                EventLogger.WriteEventLogEntry(ex.Message,System.Diagnostics.EventLogEntryType.Error);
                return null;
            }

            if (returnCode != 0)
            {
                return null;
            }
            string[] destinationIPAddressStringArray = new string[(int)destinationIPAddressByteArrayLength];
            for (int i = 0; i < destinationIPAddressByteArrayLength; i++)
            {
                destinationIPAddressStringArray[i] = destinationIPAddressByteArray[i].ToString("X2");
            }
            string maxAddress = string.Join("-", destinationIPAddressStringArray); 
            return maxAddress;
        }

       /* public async Task<string> LookupMac(string MacAddress)
        {
            var uri = new Uri("http://api.macvendors.com/" + WebUtility.UrlEncode(MacAddress));
            using (var wc = new HttpClient())
                return await wc.GetStringAsync(uri);
        }*/

        public async Task<string> LookupMac(string MacAddress)
        {
            return "";
            var uri = new Uri("http://api.macvendors.com/" + WebUtility.UrlEncode(MacAddress));
            using (var wc = new HttpClient())
                return await wc.GetStringAsync(uri);
        }

    }

    public class IPInfo 
    {
        private string ip;
        private int port;
        private string systemName;
        private string roundTime;
        private bool alive;
        private string commitDate;
        private string description;
        private string macaddr;
        private string vendor;

        public string Ip
        {
            get => ip; 
            set
            {
                ip = value; 
            }
        }
        public int Port { get => port; set => port = value; }
        public string SystemName { get => systemName; set => systemName = value; }
        public string RountTime { get => roundTime; set => roundTime = value; }
        public bool Alive { get => alive; set => alive = value; }
        public string CommitDate { get => commitDate; set => commitDate = value; }
        public string Description { get => description; set => description = value; }
        public string Macaddr { get => macaddr; set => macaddr = value; }
        public string Vendor { get => vendor; set => vendor = value; }
        public object RoundTime { get; internal set; }
    }
}
