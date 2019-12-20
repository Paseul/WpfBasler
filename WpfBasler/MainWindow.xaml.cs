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
using System.IO;
using System.Drawing;
using System.Windows.Threading;


namespace WpfBasler
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private Camera camera;
        private bool grabbing;
        private PixelDataConverter converter = new PixelDataConverter();
        Dispatcher dispatcher = Application.Current.Dispatcher;
        OpenCvSharp.VideoWriter videoWriter = new OpenCvSharp.VideoWriter();
        bool isWrite = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
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

        public bool snapImage(string path, int height = 0, int width = 0)
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

                        Mat gray = new Mat();
                        Mat binary = new Mat();
                        Mat morp = new Mat();
                        Mat canny = new Mat();
                        Mat dst = img.Clone();

                        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));

                        Cv2.CvtColor(img, gray, ColorConversionCodes.BGR2GRAY);
                        Cv2.Threshold(gray, binary, 150, 255, ThresholdTypes.Binary);
                        Cv2.Dilate(binary, morp, kernel, new OpenCvSharp.Point(-1, -1));
                        Cv2.Erode(morp, morp, kernel, new OpenCvSharp.Point(-1, -1), 3);
                        Cv2.Dilate(morp, morp, kernel, new OpenCvSharp.Point(-1, -1), 2);
                        Cv2.Canny(morp, canny, 0, 0, 3);

                        LineSegmentPoint[] lines = Cv2.HoughLinesP(canny, 1, Cv2.PI / 90, 10, 10, 10);

                        for (int i = 0; i < lines.Length; i++)
                        {
                            Cv2.Line(dst, lines[i].P1, lines[i].P2, Scalar.Red, 10);
                        }


                        // save image
                        Cv2.ImWrite(path, dst);
                        // resize image  to fit the imageBox
                        Cv2.Resize(dst, dst, new OpenCvSharp.Size(960, 687), 0, 0, InterpolationFlags.Linear);
                        // copy processed image to imgCamera.Source
                        imgCamera.Source = dst.ToWriteableBitmap(PixelFormats.Bgr24); 
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
            return new Mat(grabResult.Height, grabResult.Width, MatType.CV_8U, buffer);
        }    
        
        private Mat houghCircles(Mat img)
        {
            Mat image = new Mat();
            Mat dst = img.Clone();

            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));

            Cv2.CvtColor(img, image, ColorConversionCodes.BGR2GRAY);
            Cv2.Dilate(image, image, kernel, new OpenCvSharp.Point(-1, -1), 3);
            Cv2.GaussianBlur(image, image, new OpenCvSharp.Size(13, 13), 3, 3, BorderTypes.Reflect101);
            Cv2.Erode(image, image, kernel, new OpenCvSharp.Point(-1, -1), 3);

            CircleSegment[] circles = Cv2.HoughCircles(image, HoughMethods.Gradient, 4, 100, 100, 30, 0, 0);

            for (int i = 0; i < circles.Length; i++)
            {
                OpenCvSharp.Point center = new OpenCvSharp.Point(circles[i].Center.X, circles[i].Center.Y);

                Cv2.Circle(dst, center, (int)circles[i].Radius, Scalar.White, 3);
                Cv2.Circle(dst, center, 5, Scalar.AntiqueWhite, Cv2.FILLED);
            }
            return dst;
        }

        private void th_grab(int height = 0, int width = 0, int snap_wait = 500)
        {
            try
            {
                // Set the acquisition mode to free running continuous acquisition when the camera is opened.
                camera.CameraOpened += Configuration.AcquireContinuous;

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
                if (isWrite)
                {
                    var expected = new OpenCvSharp.Size(1920, 1374);
                    string filename = DateTime.Now.ToString("M.dd-HH.mm.ss") + ".avi";
                    videoWriter.Open("video.avi", OpenCvSharp.FourCCValues.XVID, 14, expected, true);
                }                
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
                            //img = Cv2.ImRead("colorball.png");
                            //img = houghCircles(img);
                            Cv2.Resize(img, img, new OpenCvSharp.Size(1920, 1374), 0, 0, InterpolationFlags.Linear);
                            if (isWrite)
                                videoWriter.Write(img);
                            // resize image  to fit the imageBox
                            Cv2.Resize(img, img, new OpenCvSharp.Size(960, 687), 0, 0, InterpolationFlags.Linear);
                            // copy processed image to imagebox.image
                            Bitmap bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(img);

                            BitmapToImageSource(bitmap);
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("Error: {0} {1}" + grabResult.ErrorCode, grabResult.ErrorDescription);
                        }
                    }

                    Thread.Sleep(snap_wait);
                }
                videoWriter.Release();
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

        void BitmapToImageSource(Bitmap bitmap)
        {
            //UI thread에 접근하기 위해 dispatcher 사용
            dispatcher.BeginInvoke((Action)(() =>
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                    memory.Position = 0;
                    BitmapImage bitmapimage = new BitmapImage();
                    bitmapimage.BeginInit();
                    bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapimage.StreamSource = memory;
                    bitmapimage.EndInit();
                    imgCamera.Source = bitmapimage;     
                }
            }));
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            BaslerCamera("192.168.1.6");
        }

        private void btnOneShot_Click(object sender, RoutedEventArgs e)
        {
            string filename = DateTime.Now.ToString("M.dd-HH.mm.ss") + ".jpg";
            snapImage(filename, 2748, 3840);
        }

        private void btnConShot_Click(object sender, RoutedEventArgs e)
        {
            if (!grabbing)
            {
                grabbing = true;

                try
                {
                    Thread thread = new Thread(() => th_grab(2748, 3840, 0));
                    thread.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void btnDisConnect_Click(object sender, RoutedEventArgs e)
        {
            grabbing = false;
            isWrite = false;
        }

        private void sliderGain_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            camera.Parameters[PLCamera.Gain].SetValue(sliderGain.Value);
        }

        private void checkSave_Checked(object sender, RoutedEventArgs e)
        {
            isWrite = true;
        }
    }
}
