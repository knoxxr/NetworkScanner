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

        public void AddItem(ScanRangeInfo item)
        {
            if (!IsExist(item.StartIP, item.EndIP))
            {
                this.Add(item);
            }
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

        public bool IsExist(string startIp, string endIp)
        {
            if (this.Where(x => x.StartIP == startIp).FirstOrDefault() == null && this.Where(x => x.EndIP == endIp).FirstOrDefault() == null)
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
    
}
