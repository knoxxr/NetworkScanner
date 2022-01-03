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
    }

    public class IPInfo 
    {
        private string ip;
        private int port;
        private string systemName;
        private long rountTime;
        private bool alive;
        private string commitDate;
        private string description;

        public string Ip
        {
            get => ip; 
            set
            {
                ip = value; 
                //NotifyPropertyChanged();
            }
        }
        public int Port { get => port; set => port = value; }
        public string SystemName { get => systemName; set => systemName = value; }
        public long RountTime { get => rountTime; set => rountTime = value; }
        public bool Alive { get => alive; set => alive = value; }
        public string CommitDate { get => commitDate; set => commitDate = value; }
        public string Description { get => description; set => description = value; }

       
    }
}
