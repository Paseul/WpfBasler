using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace WpfBasler
{
    class CvProcess
    {
        public Mat histogram(Mat src)
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
                    if (dX > 0 && dY > 0)
                        Cv2.Ellipse(dst, center, axis, theta, 0, 360, Scalar.White);
                }
            }

            return dst;
        }

        public Mat fourier(Mat img)
        {
            Mat padded = new Mat();
            int m = Cv2.GetOptimalDFTSize(img.Rows);
            int n = Cv2.GetOptimalDFTSize(img.Cols); // on the border add zero values
            Cv2.CopyMakeBorder(img, padded, 0, m - img.Rows, 0, n - img.Cols, BorderTypes.Constant, Scalar.All(0));

            // Add to the expanded another plane with zeros
            Mat paddedF32 = new Mat();
            padded.ConvertTo(paddedF32, MatType.CV_32F);
            Mat[] planes = { paddedF32, Mat.Zeros(padded.Size(), MatType.CV_32F) };
            Mat complex = new Mat();
            Cv2.Merge(planes, complex);

            // this way the result may fit in the source matrix
            Mat dft = new Mat();
            Cv2.Dft(complex, dft);

            // compute the magnitude and switch to logarithmic scale
            // => log(1 + sqrt(Re(DFT(I))^2 + Im(DFT(I))^2))
            Mat[] dftPlanes;
            Cv2.Split(dft, out dftPlanes);  // planes[0] = Re(DFT(I), planes[1] = Im(DFT(I))

            // planes[0] = magnitude
            Mat magnitude = new Mat();
            Cv2.Magnitude(dftPlanes[0], dftPlanes[1], magnitude);

            magnitude += Scalar.All(1);  // switch to logarithmic scale
            Cv2.Log(magnitude, magnitude);

            // crop the spectrum, if it has an odd number of rows or columns
            Mat spectrum = magnitude[
                new OpenCvSharp.Rect(0, 0, magnitude.Cols & -2, magnitude.Rows & -2)];

            // rearrange the quadrants of Fourier image  so that the origin is at the image center
            int cx = spectrum.Cols / 2;
            int cy = spectrum.Rows / 2;

            Mat q0 = new Mat(spectrum, new OpenCvSharp.Rect(0, 0, cx, cy));   // Top-Left - Create a ROI per quadrant
            Mat q1 = new Mat(spectrum, new OpenCvSharp.Rect(cx, 0, cx, cy));  // Top-Right
            Mat q2 = new Mat(spectrum, new OpenCvSharp.Rect(0, cy, cx, cy));  // Bottom-Left
            Mat q3 = new Mat(spectrum, new OpenCvSharp.Rect(cx, cy, cx, cy)); // Bottom-Right

            // swap quadrants (Top-Left with Bottom-Right)
            Mat tmp = new Mat();
            q0.CopyTo(tmp);
            q3.CopyTo(q0);
            tmp.CopyTo(q3);

            // swap quadrant (Top-Right with Bottom-Left)
            q1.CopyTo(tmp);
            q2.CopyTo(q1);
            tmp.CopyTo(q2);

            // Transform the matrix with float values into a
            Cv2.Normalize(spectrum, spectrum, 0, 1, NormTypes.MinMax);

            // calculating the idft
            Mat inverseTransform = new Mat();
            Cv2.Dft(dft, inverseTransform, DftFlags.Inverse | DftFlags.RealOutput);
            Cv2.Normalize(inverseTransform, inverseTransform, 0, 1, NormTypes.MinMax);

            double minVal = 0.0, maxVal = 0.0;
            Cv2.MinMaxIdx(inverseTransform, out minVal, out maxVal);
            Cv2.ConvertScaleAbs(inverseTransform, inverseTransform, 255.0 / (maxVal - minVal), -minVal * 255.0 / (maxVal - minVal));

            return inverseTransform;
        }

        public Mat houghLines(Mat img)
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
            if (lines.Length > 0)
            {
                pointX = pointX / (lines.Length * 2);
                pointY = pointY / (lines.Length * 2);

                text = pointX.ToString();
                text = text + ":" + pointY.ToString();
                Cv2.PutText(dst, text, new OpenCvSharp.Point(3300, 2700), HersheyFonts.HersheyPlain, 5, Scalar.White, 5);
            }

            return dst;
        }

        public Mat MinEnclosing(Mat img)
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
    }
}
