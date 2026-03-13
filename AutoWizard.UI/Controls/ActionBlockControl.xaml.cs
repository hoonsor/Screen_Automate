using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AutoWizard.Core.Models;
using AutoWizard.Core.Actions.Input;
using AutoWizard.Core.Actions.Control;
using AutoWizard.Core.Actions.Vision;
using AutoWizard.UI.ViewModels;

namespace AutoWizard.UI.Controls
{
    public partial class ActionBlockControl : UserControl
    {
        public ActionBlockControl()
        {
            InitializeComponent();
        }
    }

    #region Value Converters

    /// <summary>
    /// LeftIndent (double) → Thickness (margin-left)
    /// </summary>
    public class LeftIndentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double indent)
                return new Thickness(indent, 0, 0, 0);
            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// NodeType → Visibility（只在 ConverterParameter 匹配時顯示）
    /// </summary>
    public class NodeTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NodeType nodeType && parameter is string expected)
            {
                return nodeType.ToString() == expected ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// BaseAction → 色帶顏色
    /// </summary>
    public class ActionToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                ClickAction => "#4CAF50",
                TypeAction => "#4CAF50",
                KeyboardAction => "#4CAF50",
                IfAction => "#FF9800",
                LoopAction => "#FF9800",
                SetVariableAction => "#FF9800",
                FindImageAction => "#2196F3",
                OCRAction => "#2196F3",
                WaitAction => "#9C27B0",
                ScreenshotAction => "#9C27B0",
                _ => "#9E9E9E"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// BaseAction → 圖示
    /// </summary>
    public class ActionToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                ClickAction => "🖱️",
                TypeAction => "⌨️",
                KeyboardAction => "🎹",
                IfAction => "🔀",
                LoopAction => "🔁",
                SetVariableAction => "📌",
                FindImageAction => "🔍",
                OCRAction => "📝",
                WaitAction => "⏳",
                ScreenshotAction => "📷",
                _ => "⚡"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// BaseAction → 類型名稱
    /// </summary>
    public class ActionToTypeNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                ClickAction => "Click",
                TypeAction => "Type",
                KeyboardAction => "Keyboard",
                IfAction => "If",
                LoopAction => "Loop",
                SetVariableAction => "SetVariable",
                FindImageAction => "FindImage",
                OCRAction => "OCR",
                WaitAction => "Wait",
                ScreenshotAction => "Screenshot",
                _ => value?.GetType().Name ?? ""
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// BaseAction → 參數摘要
    /// </summary>
    public class ActionToSummaryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                ClickAction c => $"X:{c.X} Y:{c.Y} {c.Button} {c.ClickType}",
                TypeAction t => t.Text.Length > 30 ? t.Text[..30] + "..." : t.Text,
                KeyboardAction k => $"{k.Modifiers} + {k.Key}",
                IfAction i => i.Conditions.Count > 1 
                    ? $"[{i.Conditions.Count} 條件] {i.ConditionRelation}" 
                    : $"{i.ConditionType}: {i.LeftOperand} → {i.RightOperand}",
                LoopAction l => l.LoopType == LoopType.Count ? $"次數: {l.Count}" : $"{l.LoopType}",
                SetVariableAction s => $"{s.VariableName} = {s.ValueExpression}",
                FindImageAction f => System.IO.Path.GetFileName(f.TemplateImagePath),
                OCRAction o => string.IsNullOrEmpty(o.SearchText) ? $"語言: {o.Language}" : $"搜尋: {o.SearchText}",
                WaitAction w => w.WaitType == WaitType.Random ? $"隨機: {w.RandomMinMs}-{w.RandomMaxMs}ms" : (string.IsNullOrEmpty(w.DurationExpression) ? $"{w.DurationMs}ms" : w.DurationExpression),
                ScreenshotAction sc => string.IsNullOrEmpty(sc.SavePath) ? "擷取至變數" : sc.SavePath,
                _ => ""
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// BaseAction → 容器標記（如「📦」）
    /// </summary>
    public class ActionToContainerBadgeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ContainerAction)
                return "📦";
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// bool (IsCut) → Opacity（true = 0.4，false = 1.0）
    /// </summary>
    public class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCut && isCut)
                return 0.4;
            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    #endregion
}
