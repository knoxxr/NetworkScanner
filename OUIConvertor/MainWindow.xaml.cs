using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OUIConvertor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WebOUIConvertor ouiconverter = new WebOUIConvertor();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ouiconverter.ProgressChanged += Ouiconverter_ProgressChanged;
            ouiconverter.ProgressCompleted += Ouiconverter_ProgressCompleted;
        }

        private void Ouiconverter_ProgressCompleted(int value)
        {
            ProgBar.Value = 0;
            lbProgress.Content = string.Format("완료 : {0}", value);
        }

        private void Ouiconverter_ProgressChanged(int value)
        {
            ProgBar.Value = value;
            lbProgress.Content = value.ToString();  
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ouiconverter.Initialize();
        }
    }
}
