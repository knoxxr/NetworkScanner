using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NetworkScanner
{
    public class IPInformationListViewModel
    {
        private readonly IPInformationList items;

        public IPInformationListViewModel()
        {
            this.items = new IPInformationList();
        }

        public IPInformationList Items
        {
            get { return this.items; }
        }
    }

    public class IPInformationList : ObservableCollection<IPInfo>
    {

    }

    public class IPInfo : INotifyPropertyChanged
    {
        private IPAddress ip;
        private int port;
        private string systemName;
        private long rountTime;
        private bool alive;
        private DateTime commitDate;
        private string description;

        public IPAddress Ip { get => ip; set => ip = value; }
        public int Port { get => port; set => port = value; }
        public string SystemName { get => systemName; set => systemName = value; }
        public long RountTime { get => rountTime; set => rountTime = value; }
        public bool Alive { get => alive; set => alive = value; }
        public DateTime CommitDate { get => commitDate; set => commitDate = value; }
        public string Description { get => description; set => description = value; }

        private static List<IPInfo> instance;

        public static List<IPInfo> GetInstance()
        {
            if (instance == null)
                instance = new List<IPInfo>();

            return instance;
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
}
