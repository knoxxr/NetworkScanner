using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NetworkScanner
{
    public class IPRange
    {
        public class IPRangeInfo : INotifyPropertyChanged
        {
            public int Index { get; set; }
            public IPAddress StartIP { get; set; }
            public IPAddress EndIP { get; set; }
            public string Description { get; set; }

            private static List<IPRangeInfo> instance;

            public static List<IPRangeInfo> GetInstance()
            {
                if (instance == null)
                    instance = new List<IPRangeInfo>();

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
}
