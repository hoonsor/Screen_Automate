using System;
using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace AutoWizard.CV.Vision
{
    /// <summary>
    /// 影像辨識結果
    /// </summary>
    public class MatchResult
    {
        public bool Found { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public double Confidence { get; set; }
        public Rectangle Region { get; set; }
    }

    /// <summary>
    /// OpenCV 影像辨識引擎
    /// </summary>
    public class ImageMatcher
    {
        /// <summary>
        /// 在螢幕截圖中尋找目標影像
        /// </summary>
        /// <summary>
        /// 在螢幕截圖中尋找目標影像
        /// </summary>
        public static MatchResult FindImage(Bitmap screenshot, Bitmap template, double threshold = 0.8)
        {
            try
            {
                using (var srcMat = BitmapConverter.ToMat(screenshot))
                using (var templateMat = BitmapConverter.ToMat(template))
                using (var src3 = new Mat())
                using (var templ3 = new Mat())
                using (var result = new Mat())
                {
                    // Ensure format consistency (CV_8UC3)
                    ConvertImage(srcMat, src3);
                    ConvertImage(templateMat, templ3);

                    // Check if template is larger than source
                    if (templ3.Width > src3.Width || templ3.Height > src3.Height)
                    {
                        return new MatchResult { Found = false };
                    }

                    // Template Matching
                    Cv2.MatchTemplate(src3, templ3, result, TemplateMatchModes.CCoeffNormed);
                    
                    // 找到最佳匹配位置
                    Cv2.MinMaxLoc(result, out double minVal, out double maxVal, out OpenCvSharp.Point minLoc, out OpenCvSharp.Point maxLoc);

                    if (maxVal >= threshold)
                    {
                        return new MatchResult
                        {
                            Found = true,
                            X = maxLoc.X + template.Width / 2,  // 返回中心點
                            Y = maxLoc.Y + template.Height / 2,
                            Confidence = maxVal,
                            Region = new Rectangle(maxLoc.X, maxLoc.Y, template.Width, template.Height)
                        };
                    }
                }

                return new MatchResult { Found = false };
            }
            catch (Exception ex)
            {
                throw new Exception($"Image matching failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 在螢幕截圖中尋找所有匹配的影像
        /// </summary>
        public static MatchResult[] FindAllImages(Bitmap screenshot, Bitmap template, double threshold = 0.8)
        {
            var matches = new System.Collections.Generic.List<MatchResult>();

            try
            {
                using (var srcMat = BitmapConverter.ToMat(screenshot))
                using (var templateMat = BitmapConverter.ToMat(template))
                using (var src3 = new Mat())
                using (var templ3 = new Mat())
                using (var result = new Mat())
                {
                    // Ensure format consistency (CV_8UC3)
                    ConvertImage(srcMat, src3);
                    ConvertImage(templateMat, templ3);

                    // Check for invalid dimensions
                    if (templ3.Width > src3.Width || templ3.Height > src3.Height)
                    {
                        return new MatchResult[0];
                    }

                    // Template Matching
                    Cv2.MatchTemplate(src3, templ3, result, TemplateMatchModes.CCoeffNormed);

                    // 找到所有超過閾值的匹配點
                    using (var thresholdMat = new Mat())
                    {
                        Cv2.Threshold(result, thresholdMat, threshold, 1.0, ThresholdTypes.Tozero);

                        while (true)
                        {
                            Cv2.MinMaxLoc(thresholdMat, out double minVal, out double maxVal, out OpenCvSharp.Point minLoc, out OpenCvSharp.Point maxLoc);

                            if (maxVal >= threshold)
                            {
                                matches.Add(new MatchResult
                                {
                                    Found = true,
                                    X = maxLoc.X + template.Width / 2,
                                    Y = maxLoc.Y + template.Height / 2,
                                    Confidence = maxVal,
                                    Region = new Rectangle(maxLoc.X, maxLoc.Y, template.Width, template.Height)
                                });

                                // 在該位置畫一個矩形遮蔽,避免重複偵測
                                Cv2.Rectangle(thresholdMat, 
                                    new OpenCvSharp.Point(maxLoc.X, maxLoc.Y), 
                                    new OpenCvSharp.Point(maxLoc.X + template.Width, maxLoc.Y + template.Height), 
                                    Scalar.Black, -1);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }

                return matches.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Multiple image matching failed: {ex.Message}", ex);
            }
        }

        private static void ConvertImage(Mat input, Mat output)
        {
            // Ensure 8-bit depth
            Mat working = input;
            bool disposeWorking = false;

            if (input.Depth() != MatType.CV_8U)
            {
                working = new Mat();
                input.ConvertTo(working, MatType.CV_8U);
                disposeWorking = true;
            }

            try
            {
                // Ensure 3 channels
                if (working.Channels() == 1)
                    Cv2.CvtColor(working, output, ColorConversionCodes.GRAY2BGR);
                else if (working.Channels() == 3)
                    working.CopyTo(output);
                else if (working.Channels() == 4)
                    Cv2.CvtColor(working, output, ColorConversionCodes.BGRA2BGR);
                else
                    working.CopyTo(output);
            }
            finally
            {
                if (disposeWorking) working.Dispose();
            }
        }

        /// <summary>
        /// 等待影像出現 (輪詢模式)
        /// </summary>
        public static MatchResult WaitForImage(Bitmap template, double threshold = 0.8, int timeoutMs = 30000, int intervalMs = 500)
        {
            var startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
            {
                using (var screenshot = Capture.ScreenCapture.CaptureScreen())
                {
                    var result = FindImage(screenshot, template, threshold);
                    
                    if (result.Found)
                    {
                        return result;
                    }
                }

                System.Threading.Thread.Sleep(intervalMs);
            }

            return new MatchResult { Found = false };
        }
    }
}
