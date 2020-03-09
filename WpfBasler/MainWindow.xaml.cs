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

namespace WpfBasler
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private BaslerCamera baslerCamera;        
        bool isConnect = false;        
        
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }       

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            baslerCamera = new BaslerCamera("192.168.1.6");
            baslerCamera.valueExpTime = (int)sliderExpTime.Value;
            btnConnect.Background = Brushes.LightGreen;
            isConnect = true;
        }

        private void btnDisConnect_Click(object sender, RoutedEventArgs e)
        {
            isConnect = false;
            btnConnect.Background = Brushes.LightBlue;
            baslerCamera.grabbing = false;

            checkSaveTracked.IsEnabled = true;
            checkSaveOrigin.IsEnabled = true;
            checkSaveHisto.IsEnabled = true;
            checkSaveHeatmap.IsEnabled = true;
        }

        private void btnOneShot_Click(object sender, RoutedEventArgs e)
        {
            string filename = "D:\\save\\" + DateTime.Now.ToString("M.dd-HH.mm.ss");
            baslerCamera.oneShot(filename, 2748, 3840);
        }

        private void btnConShot_Click(object sender, RoutedEventArgs e)
        {
            baslerCamera.conShot();
            checkSaveTracked.IsEnabled = false;
            checkSaveOrigin.IsEnabled = false;
            checkSaveHisto.IsEnabled = false;
            checkSaveHeatmap.IsEnabled = false;
        }        

        private void sliderGain_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {            
            baslerCamera.valueGain = (int)sliderGain.Value;
        }             

        private void sliderExpTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(isConnect)
                baslerCamera.valueExpTime = (int)sliderExpTime.Value;
        }

        private void checkSaveTracked_Checked(object sender, RoutedEventArgs e)
        {
            baslerCamera.saveTracked = true;
        }

        private void checkSaveTracked_Unchecked(object sender, RoutedEventArgs e)
        {
            baslerCamera.saveTracked = false;
        }

        private void checkSaveOrigin_Checked(object sender, RoutedEventArgs e)
        {
            baslerCamera.saveOrigin = true;
        }

        private void checkSaveOrigin_Unchecked(object sender, RoutedEventArgs e)
        {
            baslerCamera.saveOrigin = false;
        }

        private void checkSaveHisto_Checked(object sender, RoutedEventArgs e)
        {
            baslerCamera.saveHisto = true;
        }

        private void checkSaveHisto_Unchecked(object sender, RoutedEventArgs e)
        {
            baslerCamera.saveHisto = false;
        }

        private void checkSaveHeatmap_Checked(object sender, RoutedEventArgs e)
        {
            baslerCamera.saveHeatmap = true;
        }

        private void checkSaveHeatmap_Unchecked(object sender, RoutedEventArgs e)
        {
            baslerCamera.saveHeatmap = false;
        }
    }
}
