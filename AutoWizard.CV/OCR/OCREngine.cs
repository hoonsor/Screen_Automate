using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

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
    /// Windows 內建 OCR 引擎 (Windows.Media.Ocr)
    /// </summary>
    public class OCREngine : IDisposable
    {
        private Windows.Media.Ocr.OcrEngine? _engine;
        private readonly string _languageTag;

        public OCREngine(string? tessDataPath = null, string language = "chi_tra")
        {
            // Map our legacy tesseract codes to BCP-47 language tags for Windows
            _languageTag = language switch
            {
                "chi_sim" => "zh-Hans-CN",
                "eng" => "en-US",
                "chi_tra" => "zh-Hant-TW",
                _ => "zh-Hant-TW"
            };
        }

        /// <summary>
        /// 初始化 OCR 引擎
        /// </summary>
        public void Initialize()
        {
            try
            {
                var lang = new Windows.Globalization.Language(_languageTag);
                if (!Windows.Media.Ocr.OcrEngine.IsLanguageSupported(lang))
                {
                    throw new Exception($"您的 Windows 系統尚未安裝語系 '{_languageTag}' 的 OCR 支援。請至系統設定中新增該語言套件。");
                }
                
                _engine = Windows.Media.Ocr.OcrEngine.TryCreateFromLanguage(lang);
                if (_engine == null)
                {
                    throw new Exception($"Failed to create OcrEngine for language '{_languageTag}'.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"初始化 Windows OCR 引擎失敗 (語系: '{_languageTag}'): {ex.Message}", ex);
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

                // 避免 UI 執行緒死結，將 WinRT 非同步工作丟到 ThreadPool 執行
                return Task.Run(async () => await RecognizeTextAsync(targetImage, region ?? new Rectangle(0, 0, screenshot.Width, screenshot.Height))).GetAwaiter().GetResult();
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

        private async Task<OCRResult> RecognizeTextAsync(Bitmap image, Rectangle region)
        {
            using (var ms = new MemoryStream())
            {
                // 先將 System.Drawing.Bitmap 存為 Bmp
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                var imageBytes = ms.ToArray();
                
                // 轉換為 WinRT 支援的 InMemoryRandomAccessStream
                using (var ras = new InMemoryRandomAccessStream())
                {
                    using (var writer = new DataWriter(ras.GetOutputStreamAt(0)))
                    {
                        writer.WriteBytes(imageBytes);
                        await writer.StoreAsync();
                    }
                    
                    // 解碼為 SoftwareBitmap
                    var decoder = await BitmapDecoder.CreateAsync(ras);
                    using (var softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied))
                    {
                        var ocrResult = await _engine!.RecognizeAsync(softwareBitmap);
                        
                        string processedText = string.IsNullOrWhiteSpace(ocrResult.Text) ? string.Empty : ocrResult.Text.Trim();
                        // 將中文字與中文字之間的空白移除
                        if (!string.IsNullOrEmpty(processedText))
                        {
                            processedText = Regex.Replace(processedText, @"(?<=[\u4e00-\u9fa5])\s+(?=[\u4e00-\u9fa5])", "");
                        }

                        return new OCRResult
                        {
                            Success = !string.IsNullOrWhiteSpace(processedText),
                            Text = processedText,
                            Confidence = 1.0f, // Windows OCR 不提供直接的整體信心指數，若辨識成功視為 1.0
                            Region = region
                        };
                    }
                }
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
            // Windows OcrEngine doesn't need explicit disposal mapping
            _engine = null;
        }
    }
}
