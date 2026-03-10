using System;
using System.Drawing;
using System.Text.RegularExpressions;
using Tesseract;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace AutoWizard.CV.OCR
{
    /// <summary>
    /// OCR 辨識結果
    /// </summary>
    public class OCRResult
    {
        public bool Success { get; set; }
        public string Text { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public Rectangle Region { get; set; }
    }

    /// <summary>
    /// Tesseract OCR 引擎
    /// </summary>
    public class OCREngine : IDisposable
    {
        private TesseractEngine? _engine;
        private readonly string _tessDataPath;
        private readonly string _language;

        public OCREngine(string tessDataPath = "./tessdata", string language = "chi_tra")
        {
            _tessDataPath = tessDataPath;
            _language = language;
        }

        /// <summary>
        /// 初始化 OCR 引擎
        /// </summary>
        public void Initialize()
        {
            try
            {
                _engine = new TesseractEngine(_tessDataPath, _language, EngineMode.Default);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to initialize OCR engine: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 辨識螢幕截圖中的文字
        /// </summary>
        public OCRResult RecognizeText(Bitmap screenshot, Rectangle? region = null)
        {
            if (_engine == null)
            {
                Initialize();
            }

            try
            {
                // 如果指定區域,裁切圖片
                Bitmap targetImage = screenshot;
                if (region.HasValue)
                {
                    targetImage = CropImage(screenshot, region.Value);
                }

                // 預處理影像以提高辨識率
                using (var preprocessed = PreprocessImage(targetImage))
                {
                    // 將 Bitmap 轉換為 byte array
                    using (var ms = new System.IO.MemoryStream())
                    {
                        preprocessed.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        var imageBytes = ms.ToArray();
                        
                        using (var pix = Pix.LoadFromMemory(imageBytes))
                        using (var page = _engine!.Process(pix))
                        {
                            var text = page.GetText();
                            var confidence = page.GetMeanConfidence();

                            return new OCRResult
                            {
                                Success = !string.IsNullOrWhiteSpace(text),
                                Text = text.Trim(),
                                Confidence = confidence,
                                Region = region ?? new Rectangle(0, 0, screenshot.Width, screenshot.Height)
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new OCRResult
                {
                    Success = false,
                    Text = $"OCR failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 在螢幕截圖中尋找特定文字
        /// </summary>
        public OCRResult FindText(Bitmap screenshot, string searchText, bool useRegex = false)
        {
            var result = RecognizeText(screenshot);

            if (!result.Success)
            {
                return result;
            }

            bool found;
            if (useRegex)
            {
                found = Regex.IsMatch(result.Text, searchText, RegexOptions.IgnoreCase);
            }
            else
            {
                found = result.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase);
            }

            result.Success = found;
            return result;
        }

        /// <summary>
        /// 預處理影像以提高 OCR 辨識率
        /// </summary>
        private Bitmap PreprocessImage(Bitmap original)
        {
            using (var mat = BitmapConverter.ToMat(original))
            using (var gray = new Mat())
            using (var binary = new Mat())
            using (var denoised = new Mat())
            {
                // 1. 轉換為灰階
                Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);

                // 2. 二值化 (Adaptive Threshold)
                Cv2.AdaptiveThreshold(gray, binary, 255, 
                    AdaptiveThresholdTypes.GaussianC, 
                    ThresholdTypes.Binary, 11, 2);

                // 3. 去雜訊
                Cv2.FastNlMeansDenoising(binary, denoised, 10, 7, 21);

                // 4. 可選:縮放 (提高小字辨識率)
                // Cv2.Resize(denoised, denoised, new Size(), 2.0, 2.0, InterpolationFlags.Cubic);

                return BitmapConverter.ToBitmap(denoised);
            }
        }

        /// <summary>
        /// 裁切影像
        /// </summary>
        private Bitmap CropImage(Bitmap source, Rectangle cropArea)
        {
            var cropped = new Bitmap(cropArea.Width, cropArea.Height);
            using (var graphics = Graphics.FromImage(cropped))
            {
                graphics.DrawImage(source, 
                    new Rectangle(0, 0, cropArea.Width, cropArea.Height),
                    cropArea, 
                    GraphicsUnit.Pixel);
            }
            return cropped;
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }
    }
}
