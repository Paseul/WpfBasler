using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Basler.Pylon;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Threading;
using System.Windows.Threading;
using System.IO;
using Thorlabs.TL4000;

namespace WpfBasler
{
    class BaslerCamera
    {
        private Camera camera;
        private PixelDataConverter converter = new PixelDataConverter();
        Dispatcher dispatcher = Application.Current.Dispatcher;
        private CvProcess cvProcess = new CvProcess();
        OpenCvSharp.VideoWriter videoWriter = new OpenCvSharp.VideoWriter();
        OpenCvSharp.VideoWriter originWriter = new OpenCvSharp.VideoWriter();
        OpenCvSharp.VideoWriter histoWriter = new OpenCvSharp.VideoWriter();
        OpenCvSharp.VideoWriter heatmapWriter = new OpenCvSharp.VideoWriter();
        public bool grabbing;
        public bool saveTracked = false;
        public bool saveOrigin = false;
        public bool saveHisto = false;
        public bool saveHeatmap = false;
        public int valueGain;
        public int valueExpTime;
        public int axis_x;
        public int axis_y;
        public int axis_scale;
        public bool trackingLD1 = false;
        public bool trackingLD2 = false;
        bool tracking = false;
        int count = 0;
        double temp;

        //public TL4000 itc = new TL4000("USB::4883::32842::M00421760::INSTR", true, false);
        //TL4000 itc2 = new TL4000("USB::4883::32842::M00421760::INSTR", true, false);

        public BaslerCamera(string ip)
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

        public bool oneShot(string path, int height = 0, int width = 0)
        {
            try
            {
                IGrabResult grabResult = grabStart(height, width);

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

                        // Apply Histogram
                        histo = cvProcess.histogram(dst);

                        // Apply ColorMap
                        Cv2.ApplyColorMap(dst, heatmap, ColormapTypes.Rainbow);

                        // Save Original Image
                        if (saveOrigin) Cv2.ImWrite(path + ".origin.jpg", img);

                        // Background map subtraction
                        Cv2.Subtract(dst, -5, dst);

                        // save images
                        if (saveTracked) Cv2.ImWrite(path + ".jpg", dst);
                        if (saveHisto) Cv2.ImWrite(path + ".histo.jpg", histo);
                        if (saveHeatmap) Cv2.ImWrite(path + ".heatmap.jpg", heatmap);

                        // resize image  to fit the imageBox
                        Cv2.Resize(dst, dst, new OpenCvSharp.Size(960, 687), 0, 0, InterpolationFlags.Linear);
                        Cv2.Resize(heatmap, heatmap, new OpenCvSharp.Size(256, 183), 0, 0, InterpolationFlags.Linear);

                        // display images
                        BitmapToImageSource(dst);
                        BitmapHistoToImageSource(histo);
                        BitmapHeatmapToImageSource(heatmap);
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

        private IGrabResult grabStart(int height = 0, int width = 0)
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

        public void conShot()
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

                if (saveTracked)
                {
                    var expected = new OpenCvSharp.Size(1920, 1374);
                    string filename = "D:\\save\\" + DateTime.Now.ToString("M.dd-HH.mm.ss") + ".avi";
                    videoWriter.Open(filename, OpenCvSharp.FourCCValues.XVID, 14, expected, false);
                }
                if (saveOrigin)
                {
                    var expected = new OpenCvSharp.Size(1920, 1374);
                    string filename = "D:\\save\\" + DateTime.Now.ToString("M.dd-HH.mm.ss") + ".origin.avi";
                    originWriter.Open(filename, OpenCvSharp.FourCCValues.XVID, 14, expected, false);
                }
                if (saveHisto)
                {
                    var expected = new OpenCvSharp.Size(256, 300);
                    string filename = "D:\\save\\" + DateTime.Now.ToString("M.dd-HH.mm.ss") + ".histo.avi";
                    histoWriter.Open(filename, OpenCvSharp.FourCCValues.XVID, 14, expected, false);
                }
                if (saveHeatmap)
                {
                    var expected = new OpenCvSharp.Size(1920, 1374);
                    string filename = "D:\\save\\" + DateTime.Now.ToString("M.dd-HH.mm.ss") + ".heatmap.avi";
                    heatmapWriter.Open(filename, OpenCvSharp.FourCCValues.XVID, 14, expected, true);
                }

                while (grabbing)
                {
                    camera.Parameters[PLCamera.Gain].SetValue(valueGain);
                    camera.Parameters[PLCamera.ExposureTime].SetValue(valueExpTime);
                    IGrabResult grabResult = camera.StreamGrabber.RetrieveResult(5000, TimeoutHandling.ThrowException);

                    using (grabResult)
                    {
                        if (grabResult.GrabSucceeded)
                        {
                            // convert image from basler IImage to OpenCV Mat
                            Mat img = convertIImage2Mat(grabResult);

                            // convert image from BayerBG to RGB
                            Cv2.CvtColor(img, img, ColorConversionCodes.BayerBG2GRAY);
                            Cv2.Resize(img, img, new OpenCvSharp.Size(1920, 1374), 0, 0, InterpolationFlags.Linear);

                            Mat histo = new Mat();
                            Mat heatmap = new Mat();
                            Mat dst = img.Clone();

                            // Apply Histogram
                            histo = cvProcess.histogram(dst);

                            // Apply ColorMap
                            Cv2.ApplyColorMap(dst, heatmap, ColormapTypes.Rainbow);

                            // Apply Background map subtraction
                            Cv2.Subtract(dst, -5, dst);

                            if (saveOrigin) originWriter.Write(img);

                            // Create Tracked Image
                            dst = Iso11146(img, dst);

                            Cv2.Resize(dst, dst, new OpenCvSharp.Size(1920, 1374), 0, 0, InterpolationFlags.Linear);
                            if (saveTracked) videoWriter.Write(dst);
                            if (saveHisto) histoWriter.Write(histo);
                            if (saveHeatmap) heatmapWriter.Write(heatmap);

                            // resize image  to fit the imageBox                            
                            Cv2.Resize(dst, dst, new OpenCvSharp.Size(960, 687), 0, 0, InterpolationFlags.Linear);
                            Cv2.Resize(heatmap, heatmap, new OpenCvSharp.Size(256, 183), 0, 0, InterpolationFlags.Linear);

                            Cv2.Rectangle(dst, new OpenCvSharp.Rect(axis_x, axis_y, axis_scale, axis_scale), Scalar.White, 1);

                            // display images
                            BitmapToImageSource(dst);
                            BitmapHistoToImageSource(histo);
                            BitmapHeatmapToImageSource(heatmap);
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("Error: {0} {1}" + grabResult.ErrorCode, grabResult.ErrorDescription);
                        }
                    }
                    count++;
                    if(count > 500)
                    {
                        count = 0;
                        tracking = false;
                    }
                        
                    Thread.Sleep(snap_wait);
                }
                videoWriter.Release();
                originWriter.Release();
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

        private Mat convertIImage2Mat(IGrabResult grabResult)
        {
            converter.OutputPixelFormat = PixelType.BGR8packed;
            byte[] buffer = grabResult.PixelData as byte[];
            return new Mat(grabResult.Height, grabResult.Width, MatType.CV_8U, buffer);
        }

        public Mat Iso11146(Mat img, Mat dst)
        {
            Cv2.Resize(img, img, new OpenCvSharp.Size(960, 687), 0, 0, InterpolationFlags.Linear);
            Cv2.Resize(dst, dst, new OpenCvSharp.Size(960, 687), 0, 0, InterpolationFlags.Linear);

            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.Threshold(img, img, 50, 255, ThresholdTypes.Binary);
            Cv2.FindContours(img, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxTC89L1);

            foreach (OpenCvSharp.Point[] p in contours)
            {
                if (Cv2.ContourArea(p) < 1000)
                    continue;

                Moments moments = Cv2.Moments(p, true);

                if (moments.M00 != 0)
                {
                    int cX = (int)(moments.M10 / moments.M00);
                    int cY = (int)(moments.M01 / moments.M00);
                    int cX2 = (int)(moments.Mu20 / moments.M00);
                    int cXY = (int)(moments.Mu11 / moments.M00);
                    int cY2 = (int)(moments.Mu02 / moments.M00);

                    double a = Math.Pow(((cX2 + cY2) + 2 * Math.Abs(cXY)), 0.5);
                    int dX = (int)(2 * Math.Pow(2, 0.5) * Math.Pow(((cX2 + cY2) + 2 * Math.Abs(cXY)), 0.5));
                    int dY = (int)(2 * Math.Pow(2, 0.5) * Math.Pow(((cX2 + cY2) - 2 * Math.Abs(cXY)), 0.5));

                    double t;
                    if ((cX2 - cY2) != 0)
                        t = 2 * cXY / (cX2 - cY2);
                    else
                        t = 0;

                    double theta = 0.5 * Math.Atan(t) * 180;
                    OpenCvSharp.Point center = new OpenCvSharp.Point(cX, cY);
                    OpenCvSharp.Size axis = new OpenCvSharp.Size(dX, dY);
                    Cv2.Circle(dst, cX, cY, 1, Scalar.Black);
                    /*if (trackingLD1)
                    {
                        if (tracking == false)
                        {
                            tracking = true;

                            if ((cX - (axis_x + axis_scale/2)) > 10)
                            {       
                                itc.getTecCurrSetpoint(0, out temp);
                                itc.setTecCurrSetpoint(temp - 0.005);
                            }
                            else if ((cX - (axis_x + axis_scale / 2)) < -10)
                            {
                                itc.getTecCurrSetpoint(0, out temp);
                                itc.setTecCurrSetpoint(temp + 0.005);
                            }
                        }
                    }*/

                    if (dX > 0 && dY > 0)
                        Cv2.Ellipse(dst, center, axis, theta, 0, 360, Scalar.White);
                }
            }

            return dst;
        }

        void BitmapToImageSource(Mat dst)
        {
            Bitmap bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(dst);
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
                    ((MainWindow)Application.Current.MainWindow).imgCamera.Source = bitmapimage;
                }
            }));
        }

        void BitmapHistoToImageSource(Mat histo)
        {
            Bitmap bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(histo);
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
                    ((MainWindow)Application.Current.MainWindow).imgHisto.Source = bitmapimage;
                }
            }));
        }

        void BitmapHeatmapToImageSource(Mat heatmap)
        {
            Bitmap bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(heatmap);
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
                    ((MainWindow)Application.Current.MainWindow).imgHeatmap.Source = bitmapimage;
                }
            }));
        }
    }
}
