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
        OpenCvSharp.VideoWriter histoWriter = new OpenCvSharp.VideoWriter();
        OpenCvSharp.VideoWriter heatmapWriter = new OpenCvSharp.VideoWriter();
        bool isWrite = false;
        bool isHoughLines = false;
        bool isMinEnclosing = false;
        bool isClahe = false;
        bool isEqualize = false;
        int valueErode;

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
                        Cv2.CvtColor(img, img, ColorConversionCodes.BayerBG2GRAY);

                        Mat histo = new Mat();
                        Mat heatmap = new Mat();
                        Mat dst = img.Clone();

                        if (isClahe)
                        {
                            CLAHE clahe = Cv2.CreateCLAHE();
                            clahe = Cv2.CreateCLAHE(clipLimit: 2.0,  tileGridSize: new OpenCvSharp.Size(16.0, 16.0));
                            clahe.Apply(dst, dst);
                        }
                        
                        if(isEqualize)
                            Cv2.EqualizeHist(dst, dst);

                        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(9, 9));
                        Cv2.Erode(dst, dst, kernel, new OpenCvSharp.Point(-1, -1), (int)sliderErode.Value, BorderTypes.Reflect101, new Scalar(0));

                        Cv2.ApplyColorMap(dst, heatmap, ColormapTypes.Rainbow);

                        histo = histogram(dst);

                        if (isMinEnclosing)
                            dst = MinEnclosing(dst);

                        if (isHoughLines)
                            dst = houghLines(dst);

                        // save image
                        Cv2.ImWrite(path + ".jpg", dst);
                        Cv2.ImWrite(path + ".histo.jpg", histo);
                        Cv2.ImWrite(path + ".heatmap.jpg", heatmap);

                        // resize image  to fit the imageBox
                        Cv2.Resize(dst, dst, new OpenCvSharp.Size(960, 687), 0, 0, InterpolationFlags.Linear);
                        Cv2.Resize(heatmap, heatmap, new OpenCvSharp.Size(256, 183), 0, 0, InterpolationFlags.Linear);

                        // copy processed image to imgCamera.Source
                        imgCamera.Source = dst.ToWriteableBitmap(PixelFormats.Gray8);
                        imgHisto.Source = histo.ToWriteableBitmap(PixelFormats.Gray8);
                        imgHeatmap.Source = heatmap.ToWriteableBitmap(PixelFormats.Bgr24);
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

        private Mat histogram(Mat src)
        {
            Mat hist = new Mat();
            Mat result = Mat.Ones(new OpenCvSharp.Size(256, 300), MatType.CV_8UC1);

            Cv2.CalcHist(new Mat[] { src }, new int[] { 0 }, null, hist, 1, new int[] { 256 }, new Rangef[] { new Rangef(0, 256) });
            Cv2.Normalize(hist, hist, 0, 255, NormTypes.MinMax);

            for (int i = 0; i < hist.Rows; i++)
            {
                Cv2.Line(result, new OpenCvSharp.Point(i, 300), new OpenCvSharp.Point(i, 300 - hist.Get<float>(i)), Scalar.White);
            }

            return result;            
        }

        private Mat houghLines(Mat img)
        {
            Mat binary = new Mat();
            Mat morp = new Mat();
            Mat canny = new Mat();
            Mat dst = img.Clone();
            int pointX = 0, pointY = 0;
            string text;

            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));

            Cv2.Threshold(img, binary, 150, 255, ThresholdTypes.Binary);
            Cv2.Dilate(binary, morp, kernel, new OpenCvSharp.Point(-1, -1));
            Cv2.Erode(morp, morp, kernel, new OpenCvSharp.Point(-1, -1), 3);
            Cv2.Dilate(morp, morp, kernel, new OpenCvSharp.Point(-1, -1), 2);
            Cv2.Canny(morp, canny, 0, 0, 3);

            LineSegmentPoint[] lines = Cv2.HoughLinesP(canny, 1, Cv2.PI / 90, 10, 10, 10);

            for (int i = 0; i < lines.Length; i++)
            {
                Cv2.Line(dst, lines[i].P1, lines[i].P2, Scalar.Red, 10);
                pointX += lines[i].P1.X + lines[i].P2.X;
                pointY += lines[i].P1.Y + lines[i].P2.Y;
            }
            if(lines.Length > 0) 
            {
                pointX = pointX / (lines.Length * 2);
                pointY = pointY / (lines.Length * 2) ;

                text = pointX.ToString();
                text = text + ":" + pointY.ToString();
                Cv2.PutText(dst, text, new OpenCvSharp.Point(3300, 2700), HersheyFonts.HersheyPlain, 5, Scalar.White, 5);
            }           

            return dst;
        }

        private Mat MinEnclosing(Mat img)
        {
            Mat binary = new Mat();
            Mat morp = new Mat();
            Mat image = new Mat();
            Mat dst = img.Clone();
            int pointX = 0, pointY = 0;
            string text;

            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;

            Cv2.Threshold(img, binary, 230, 255, ThresholdTypes.Binary);
            Cv2.MorphologyEx(binary, morp, MorphTypes.Close, kernel, new OpenCvSharp.Point(-1, -1), 2);
            Cv2.BitwiseNot(morp, image);

            Cv2.FindContours(image, out contours, out hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxTC89KCOS);

            for (int i = 0; i < contours.Length; i++)
            {
                double perimeter = Cv2.ArcLength(contours[i], true);
                double epsilon = perimeter * 0.01;

                OpenCvSharp.Point[] approx = Cv2.ApproxPolyDP(contours[i], epsilon, true);
                OpenCvSharp.Point[][] draw_approx = new OpenCvSharp.Point[][] { approx };
                Cv2.DrawContours(dst, draw_approx, -1, new Scalar(255, 0, 0), 2, LineTypes.AntiAlias);

                Cv2.MinEnclosingCircle(contours[i], out Point2f center, out float radius);
                Cv2.Circle(dst, new OpenCvSharp.Point(center.X, center.Y), (int)radius, Scalar.Red, 2, LineTypes.AntiAlias);

                pointX += (int)center.X;
                pointY += (int)center.Y;

                for (int j = 0; j < approx.Length; j++)
                {
                    Cv2.Circle(dst, approx[j], 1, new Scalar(0, 0, 255), 3);
                }
            }
                                 
            if (contours.Length > 0)
            {
                pointX = pointX / contours.Length;
                pointY = pointY / contours.Length;

                text = pointX.ToString();
                text = text + ":" + pointY.ToString();
                Cv2.PutText(dst, text, new OpenCvSharp.Point(3300, 2700), HersheyFonts.HersheyPlain, 5, Scalar.White, 5);
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
                    string filename = "D:\\save\\" + DateTime.Now.ToString("M.dd-HH.mm.ss") + ".avi";
                    videoWriter.Open(filename, OpenCvSharp.FourCCValues.XVID, 14, expected, false);

                    expected = new OpenCvSharp.Size(256, 300);
                    filename = "D:\\save\\" + DateTime.Now.ToString("M.dd-HH.mm.ss") + ".histo.avi";
                    histoWriter.Open(filename, OpenCvSharp.FourCCValues.XVID, 14, expected, false);

                    expected = new OpenCvSharp.Size(1920, 1374);
                    filename = "D:\\save\\" + DateTime.Now.ToString("M.dd-HH.mm.ss") + ".heatmap.avi";
                    heatmapWriter.Open(filename, OpenCvSharp.FourCCValues.XVID, 14, expected, true);
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
                            Cv2.CvtColor(img, img, ColorConversionCodes.BayerBG2GRAY);  

                            Mat histo = new Mat();
                            Mat heatmap = new Mat();
                            Mat dst = img.Clone();                        

                            if (isClahe)
                            {
                                CLAHE clahe = Cv2.CreateCLAHE();
                                clahe = Cv2.CreateCLAHE(clipLimit: 2.0, tileGridSize: new OpenCvSharp.Size(16.0, 16.0));
                                clahe.Apply(dst, dst);
                            }

                            if (isEqualize)
                                Cv2.EqualizeHist(dst, dst);

                            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(3, 3));
                            Cv2.GaussianBlur(dst, dst, new OpenCvSharp.Size(3, 3), 3, 3, BorderTypes.Reflect101);
                            Cv2.Erode(dst, dst, kernel, new OpenCvSharp.Point(-1, -1), valueErode, BorderTypes.Reflect101, new Scalar(0));

                            histo = histogram(dst);

                            Cv2.ApplyColorMap(dst, heatmap, ColormapTypes.Rainbow);                                                

                            if (isMinEnclosing)
                                dst = MinEnclosing(dst);                                                                                

                            if (isHoughLines)
                                dst = houghLines(dst);                            
                            
                            if (isWrite)
                            {
                                Cv2.Resize(dst, dst, new OpenCvSharp.Size(1920, 1374), 0, 0, InterpolationFlags.Linear);
                                Cv2.Resize(heatmap, heatmap, new OpenCvSharp.Size(1920, 1374), 0, 0, InterpolationFlags.Linear);

                                videoWriter.Write(dst);
                                histoWriter.Write(histo);
                                heatmapWriter.Write(heatmap);
                            }                     
                                
                            // resize image  to fit the imageBox
                            Cv2.Resize(dst, dst, new OpenCvSharp.Size(960, 687), 0, 0, InterpolationFlags.Linear);
                            Cv2.Resize(heatmap, heatmap, new OpenCvSharp.Size(256, 183), 0, 0, InterpolationFlags.Linear);

                            // copy processed image to imagebox.image
                            Bitmap bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(dst);
                            Bitmap bitmapHisto = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(histo);
                            Bitmap bitmapHeatmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(heatmap);

                            BitmapToImageSource(bitmap);
                            BitmapHistoToImageSource(bitmapHisto);
                            BitmapHeatmapToImageSource(bitmapHeatmap);
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("Error: {0} {1}" + grabResult.ErrorCode, grabResult.ErrorDescription);
                        }
                    }

                    Thread.Sleep(snap_wait);
                }
                videoWriter.Release();
                histoWriter.Release();
                heatmapWriter.Release();
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

        void BitmapHistoToImageSource(Bitmap bitmap)
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
                    imgHisto.Source = bitmapimage;
                }
            }));
        }

        void BitmapHeatmapToImageSource(Bitmap bitmap)
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
                    imgHeatmap.Source = bitmapimage;
                }
            }));
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            BaslerCamera("192.168.1.6");
        }

        private void btnOneShot_Click(object sender, RoutedEventArgs e)
        {
            string filename = "D:\\save\\" + DateTime.Now.ToString("M.dd-HH.mm.ss");
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
        }

        private void sliderGain_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            camera.Parameters[PLCamera.Gain].SetValue(sliderGain.Value);
        }

        private void checkSave_Checked(object sender, RoutedEventArgs e)
        {
            isWrite = true;
        }

        private void checkSave_Unchecked(object sender, RoutedEventArgs e)
        {
            isWrite = false;
        }

        private void sliderErode_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            valueErode = (int)sliderErode.Value;
        }

        private void checkClahe_Checked(object sender, RoutedEventArgs e)
        {
            isClahe = true;
        }

        private void checkClahe_Unchecked(object sender, RoutedEventArgs e)
        {
            isClahe = false;
        }

        private void checkEqual_Checked(object sender, RoutedEventArgs e)
        {
            isEqualize = true;
        }

        private void checkEqual_Unchecked(object sender, RoutedEventArgs e)
        {
            isEqualize = false;
        }        

        private void radioHoughlines_Checked(object sender, RoutedEventArgs e)
        {
            isHoughLines = true;
        }

        private void radioHoughlines_Unchecked(object sender, RoutedEventArgs e)
        {
            isHoughLines = false;
        }

        private void radioMinenclosing_Checked(object sender, RoutedEventArgs e)
        {
            isMinEnclosing = true;
        }

        private void radioMinenclosing_Unchecked(object sender, RoutedEventArgs e)
        {
            isMinEnclosing = false;
        }

        
    }
}
