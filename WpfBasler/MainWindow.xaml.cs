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
using Thorlabs.TL4000;

namespace WpfBasler
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private BaslerCamera baslerCamera;        
        bool isConnect = false;

        TL4000 itc = new TL4000("USB::4883::32842::M00421760::INSTR", true, false);
        TL4000 itc2 = new TL4000("USB::4883::32842::M00421760::INSTR", true, false);


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

        private void sliderLD1_current_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            itc.setLdCurrSetpoint(sliderLD1_current.Value);
        }

        private void sliderLD1_temp_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            itc.setTempSetpoint(sliderLD1_temp.Value);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            itc.setLdOperatingMode(0);

            itc.setLdCurrSetpoint(sliderLD1_current.Value);
            itc.setTempSetpoint(sliderLD1_temp.Value);

            //Turn on TEC
            itc.switchTecOutput(true);
            //Turn on Laser Diode
            itc.switchLdOutput(true);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //Turn off Laser Diode
            itc.switchLdOutput(false);
            //Turn off TEC
            itc.switchTecOutput(false);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            //Turn off Laser Diode
            itc2.switchLdOutput(false);
            //Turn off TEC
            itc2.switchTecOutput(false);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            itc2.setLdOperatingMode(0);

            itc2.setLdCurrSetpoint(sliderLD2_current.Value);
            itc2.setTempSetpoint(sliderLD2_temp.Value);

            //Turn on TEC
            itc2.switchTecOutput(true);
            //Turn on Laser Diode
            itc2.switchLdOutput(true);
        }

        private void sliderLD2_current_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            itc2.setLdCurrSetpoint(sliderLD2_current.Value);
        }

        private void sliderLD2_temp_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            itc2.setTempSetpoint(sliderLD2_temp.Value);
        }
    }
}
