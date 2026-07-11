using System.Collections.Generic;
using Avalonia.Controls;

namespace NetworkScanner.Avalonia.Views
{
    public partial class RefPortListView : UserControl
    {
        public List<RefPortInfo> ReservedPortList { get; private set; } = new List<RefPortInfo>();
        public List<RefPortInfo> ProhibitPortList { get; private set; } = new List<RefPortInfo>();

        public RefPortListView()
        {
            InitializeComponent();
            LoadInfo();
        }

        public void LoadInfo()
        {
            var (reserved, prohibited) = PortReferenceLoader.Load();

            ReservedPortList = reserved;
            ProhibitPortList = prohibited;

            LbOpenPortList.ItemsSource = ReservedPortList;
            LbProhibitPortList.ItemsSource = ProhibitPortList;
        }
    }
}
