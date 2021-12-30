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
    public class IPRangeInfo
    {
        public int Index { get; set; }
        public string StartIP { get; set; }
        public string EndIP { get; set; }
        public string Description { get; set; }
    }

    public class IPRangeList : List<IPRangeInfo>
    {
        public IPRangeList()
        { 
        } 

        public void AddItem(IPRangeInfo item)
        {
            if (!IsExist(item.Index))
            {
                this.Add(item);
            }
        }

        public void DelItem(int index)
        {
            if (IsExist(index))
            {
                this.Remove(this.Where(x => x.Index == index).FirstOrDefault());
            }

        }

        public bool IsExist(int index)
        {
            if (this.Where(x => x.Index == index).FirstOrDefault() == null)
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
