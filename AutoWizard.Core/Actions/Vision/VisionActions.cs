using System;
using System.Drawing;
using AutoWizard.Core.Models;
using AutoWizard.CV.Vision;
using AutoWizard.CV.Capture;

namespace AutoWizard.Core.Actions.Vision
{
    /// <summary>
    /// 尋找影像指令
    /// </summary>
    public class FindImageAction : BaseAction
    {
        public string TemplateImagePath { get; set; } = string.Empty;
        public double Threshold { get; set; } = 0.8;
        public int TimeoutMs { get; set; } = 30000;
        public int IntervalMs { get; set; } = 500;
        public bool ClickWhenFound { get; set; } = false;
        public string SaveToVariable { get; set; } = string.Empty;

        public override ActionResult Execute(Models.ExecutionContext context)
        {
            try
            {
                // 處理相對路徑
                var imagePath = TemplateImagePath;
                if (!System.IO.Path.IsPathRooted(imagePath))
                {
                    imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath);
                }

                // 載入範本影像
                if (!System.IO.File.Exists(imagePath))
                {
                    return new ActionResult
                    {
                        Success = false,
                        Message = $"Template image not found: {TemplateImagePath} (Resolved: {imagePath})"
                    };
                }

                Bitmap template;
                try
                {
                    template = new Bitmap(imagePath);
                }
                catch (Exception ex)
                {
                    return new ActionResult
                    {
                        Success = false,
                        Message = $"Failed to load template image: {ex.Message}",
                        Exception = ex
                    };
                }

                context.Log($"Searching for image: {System.IO.Path.GetFileName(TemplateImagePath)}");

                // 等待影像出現
                var result = ImageMatcher.WaitForImage(template, Threshold, TimeoutMs, IntervalMs);

                if (!result.Found)
                {
                    return new ActionResult
                    {
                        Success = false,
                        Message = $"Image not found within {TimeoutMs}ms"
                    };
                }

                context.Log($"Image found at ({result.X}, {result.Y}) with confidence {result.Confidence:P0}");

                // 儲存座標到變數
                if (!string.IsNullOrEmpty(SaveToVariable))
                {
                    context.Variables[$"{SaveToVariable}_X"] = result.X;
                    context.Variables[$"{SaveToVariable}_Y"] = result.Y;
                    context.Variables[$"{SaveToVariable}_Confidence"] = result.Confidence;
                }

                // 如果需要,點擊找到的位置
                if (ClickWhenFound)
                {
                    var clickAction = new Input.ClickAction
                    {
                        X = result.X,
                        Y = result.Y
                    };
                    var clickResult = clickAction.Execute(context);
                    
                    if (!clickResult.Success)
                    {
                        return clickResult;
                    }
                }

                return new ActionResult
                {
                    Success = true,
                    Message = $"Image found at ({result.X}, {result.Y})",
                    Data = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["X"] = result.X,
                        ["Y"] = result.Y,
                        ["Confidence"] = result.Confidence
                    }
                };
            }
            catch (Exception ex)
            {
                return new ActionResult
                {
                    Success = false,
                    Message = $"FindImage failed: {ex.Message}",
                    Exception = ex
                };
            }
        }
    }

    /// <summary>
    /// OCR 文字辨識指令
    /// </summary>
    public class OCRAction : BaseAction
    {
        public int? RegionX { get; set; }
        public int? RegionY { get; set; }
        public int? RegionWidth { get; set; }
        public int? RegionHeight { get; set; }
        public string? SearchText { get; set; }
        public bool UseRegex { get; set; } = false;
        public string SaveToVariable { get; set; } = string.Empty;
        public string Language { get; set; } = "chi_tra";

        public override ActionResult Execute(Models.ExecutionContext context)
        {
            try
            {
                using (var screenshot = ScreenCapture.CaptureScreen())
                using (var ocrEngine = new CV.OCR.OCREngine(language: Language))
                {
                    ocrEngine.Initialize();

                    Rectangle? region = null;
                    if (RegionX.HasValue && RegionY.HasValue && RegionWidth.HasValue && RegionHeight.HasValue)
                    {
                        region = new Rectangle(RegionX.Value, RegionY.Value, RegionWidth.Value, RegionHeight.Value);
                    }

                    CV.OCR.OCRResult result;
                    
                    if (!string.IsNullOrEmpty(SearchText))
                    {
                        result = ocrEngine.FindText(screenshot, SearchText, UseRegex);
                    }
                    else
                    {
                        result = ocrEngine.RecognizeText(screenshot, region);
                    }

                    if (!result.Success)
                    {
                        return new ActionResult
                        {
                            Success = false,
                            Message = result.Text
                        };
                    }

                    context.Log($"OCR recognized: {result.Text.Substring(0, Math.Min(50, result.Text.Length))}...");

                    // 儲存結果到變數
                    if (!string.IsNullOrEmpty(SaveToVariable))
                    {
                        context.Variables[SaveToVariable] = result.Text;
                        context.Variables[$"{SaveToVariable}_Confidence"] = result.Confidence;
                    }

                    return new ActionResult
                    {
                        Success = true,
                        Message = $"OCR completed with confidence {result.Confidence:P0}",
                        Data = new System.Collections.Generic.Dictionary<string, object>
                        {
                            ["Text"] = result.Text,
                            ["Confidence"] = result.Confidence
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                return new ActionResult
                {
                    Success = false,
                    Message = $"OCR failed: {ex.Message}",
                    Exception = ex
                };
            }
        }
    }
}
