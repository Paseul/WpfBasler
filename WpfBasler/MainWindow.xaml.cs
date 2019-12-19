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
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Basler.Pylon;
using System.Threading;


namespace WpfBasler
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private Camera camera;
        private bool grabbing;
        WriteableBitmap wb;
        private PixelDataConverter converter = new PixelDataConverter();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cameraFunc(); 
        }

        private void btnOneShot_Click(object sender, RoutedEventArgs e)
        {
            //Mat img = Cv2.ImRead("colorball.png");
            //imgCamera.Source = img.ToWriteableBitmap(PixelFormats.Bgr24);
            snapImage("C:\\save\\image.jpg", 2748, 3840, 0);
        }

        private void cameraFunc()
        {
            BaslerCamera("192.168.1.6");
        }

        public void BaslerCamera(string ip)
        {
            foreach (ICameraInfo INFO in CameraFinder.Enumerate())
            {
                if (INFO.GetValueOrDefault("IpAddress", "0") == ip)
                {
                    camera = new Camera(INFO);
                    break;
                }
            }
            if (camera == null)
            {
                camera = new Camera();
            }

            grabbing = false;
        }

        public bool snapImage(string path, int height = 0, int width = 0, int imageRotation = 0)
        {
            try
            {
                IGrabResult grabResult = snap(height, width);

                using (grabResult)
                {
                    if (grabResult.GrabSucceeded)
                    {
                        // convert image from basler IImage to OpenCV Mat
                        Mat img = convertIImage2Mat(grabResult);
                        // convert image from BayerBG to RGB
                        Cv2.CvtColor(img, img, ColorConversionCodes.BayerBG2RGB);
                        // save image
                        Cv2.ImWrite(path, img);
                        // resize image  to fit the imageBox
                        int resize_width = 960;
                        int resize_height = 687;
                        Cv2.Resize(img, img, new OpenCvSharp.Size(resize_width, resize_height), 0, 0, InterpolationFlags.Linear);
                        // draw the pointer
                        //drawPointer(img, new MCvScalar(0, 100, 200), 1, Emgu.CV.CvEnum.LineType.EightConnected);
                        // copy processed image to imagebox.image
                        //WriteableBitmapConverter.ToWriteableBitmap(img, wb);
                        imgCamera.Source = img.ToWriteableBitmap(PixelFormats.Rgb24); 
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Error: {0} {1}" + grabResult.ErrorCode, grabResult.ErrorDescription);
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                if (camera.IsOpen)
                    camera.Close();

                System.Windows.MessageBox.Show("Exception: {0}" + exception.Message);

                return false;
            }
        }

        private IGrabResult snap(int height = 0, int width = 0)
        {
            // Set the acquisition mode to free running continuous acquisition when the camera is opened.
            camera.CameraOpened += Configuration.AcquireSingleFrame;

            // Open the connection to the camera device.
            camera.Open();

            if (width == 0 || width > camera.Parameters[PLCamera.Width].GetMaximum())
                camera.Parameters[PLCamera.Width].SetValue(camera.Parameters[PLCamera.Width].GetMaximum());
            else if (width < camera.Parameters[PLCamera.Width].GetMinimum())
                camera.Parameters[PLCamera.Width].SetValue(camera.Parameters[PLCamera.Width].GetMinimum());
            else
                camera.Parameters[PLCamera.Width].SetValue(width);

            if (height == 0 || width > camera.Parameters[PLCamera.Height].GetMaximum())
                camera.Parameters[PLCamera.Height].SetValue(camera.Parameters[PLCamera.Height].GetMaximum());
            else if (height < camera.Parameters[PLCamera.Height].GetMinimum())
                camera.Parameters[PLCamera.Height].SetValue(camera.Parameters[PLCamera.Height].GetMinimum());
            else
                camera.Parameters[PLCamera.Height].SetValue(height);

            camera.Parameters[PLCamera.CenterX].SetValue(true);
            camera.Parameters[PLCamera.CenterY].SetValue(true);

            camera.StreamGrabber.Start();
            IGrabResult grabResult = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);
            camera.StreamGrabber.Stop();
            camera.Close();

            return grabResult;
        }

        private Mat convertIImage2Mat(IGrabResult grabResult)
        {
            converter.OutputPixelFormat = PixelType.BGR8packed;
            byte[] buffer = grabResult.PixelData as byte[];
            //converter.Convert(buffer, grabResult);
            return new Mat(grabResult.Width, grabResult.Height, MatType.CV_8U, buffer);


            /*GCHandle pinnedArray = GCHandle.Alloc(grabResult.PixelData, GCHandleType.Pinned);
            IntPtr ptr = pinnedArray.AddrOfPinnedObject();
            pinnedArray.Free();
            return new Mat(ptr);*/
        }

        private void btnConShot_Click(object sender, RoutedEventArgs e)
        {
            if (!grabbing)
            {
                grabbing = true;

                try
                {
                    Thread thread = new Thread(() => th_grab(2748, 3840, 0, 0));
                    thread.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }           
        }

        private void th_grab(int height = 0, int width = 0, int imageRotation = 0, int snap_wait = 500)
        {
            const string videopath = "C:\\save\\Utility_GrabAvi.avi";

            try
            {
                // Set the acquisition mode to free running continuous acquisition when the camera is opened.
                camera.CameraOpened += Configuration.AcquireContinuous;

                // Open the connection to the camera device.
                camera.Open();

                if (width == 0 || width > camera.Parameters[PLCamera.Height].GetMaximum())
                    camera.Parameters[PLCamera.Width].SetValue(camera.Parameters[PLCamera.Height].GetMaximum());
                else if (width < camera.Parameters[PLCamera.Height].GetMinimum())
                    camera.Parameters[PLCamera.Width].SetValue(camera.Parameters[PLCamera.Height].GetMinimum());
                else
                    camera.Parameters[PLCamera.Width].SetValue(width);

                if (height == 0 || width > camera.Parameters[PLCamera.Height].GetMaximum())
                    camera.Parameters[PLCamera.Height].SetValue(camera.Parameters[PLCamera.Height].GetMaximum());
                else if (height < camera.Parameters[PLCamera.Height].GetMinimum())
                    camera.Parameters[PLCamera.Height].SetValue(camera.Parameters[PLCamera.Height].GetMinimum());
                else
                    camera.Parameters[PLCamera.Height].SetValue(height);

                camera.Parameters[PLCamera.CenterX].SetValue(true);
                camera.Parameters[PLCamera.CenterY].SetValue(true);

                camera.StreamGrabber.Start();

                while (grabbing)
                {
                    IGrabResult grabResult = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);

                    using (grabResult)
                    {
                        if (grabResult.GrabSucceeded)
                        {
                            // convert image from basler IImage to OpenCV Mat
                            Mat img = convertIImage2Mat(grabResult);
                            // convert image from BayerBG to RGB
                            Cv2.CvtColor(img, img, ColorConversionCodes.BayerBG2RGB);
                            // resize image  to fit the imageBox
                            int resize_width = 960;
                            int resize_height = 687;
                            Cv2.Resize(img, img, new OpenCvSharp.Size(resize_width, resize_height), 0, 0, InterpolationFlags.Linear);
                            Cv2.ImShow("image", img);
                            // draw the pointer
                            //drawPointer(img, new MCvScalar(0, 100, 200), 1, Emgu.CV.CvEnum.LineType.EightConnected);
                            // copy processed image to imagebox.image
                            //imgContinue.Source = img.ToWriteableBitmap(PixelFormats.Rgb24); ;
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("Error: {0} {1}" + grabResult.ErrorCode, grabResult.ErrorDescription);
                        }
                    }

                    Thread.Sleep(snap_wait);
                }
                camera.StreamGrabber.Stop();
                camera.Close();

            }
            catch (Exception exception)
            {
                if (camera.IsOpen)
                    camera.Close();

                System.Windows.MessageBox.Show("Exception: {0}" + exception.Message);
            }
        }
    }
}
