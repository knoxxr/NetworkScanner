using System.Collections.Generic;
using System.Windows.Controls;

namespace NetworkScanner
{
    /// <summary>
    /// UCRefPortList.xaml에 대한 상호 작용 논리
    /// </summary>
    /// 실제 파싱 로직은 NetworkScanner.Core의 PortReferenceLoader에 있다.
    public partial class UCRefPortList : UserControl
    {

        public UCRefPortList()
        {
            InitializeComponent();
            LoadInfo();
        }

        public List<RefPortInfo> ReservedPortList = new List<RefPortInfo>();
        public List<RefPortInfo> ProhibitPortList = new List<RefPortInfo>();

        public List<RefPortInfo> GetReservedPortList()
        {
            return ReservedPortList;
        }

        public List<RefPortInfo> GetProhibitPortList()
        {
            return ProhibitPortList;
        }

        public void LoadInfo()
        {
            var (reserved, prohibited) = PortReferenceLoader.Load();

            ReservedPortList = reserved;
            ProhibitPortList = prohibited;

            LBOpenPortList.Items.Clear();
            foreach (var port in ReservedPortList)
            {
                LBOpenPortList.Items.Add(port);
            }

            LBProhibitPortList.Items.Clear();
            foreach (var port in ProhibitPortList)
            {
                LBProhibitPortList.Items.Add(port);
            }
        }
    }
}
