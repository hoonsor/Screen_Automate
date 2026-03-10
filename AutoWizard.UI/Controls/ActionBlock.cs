using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AutoWizard.UI.Controls
{
    /// <summary>
    /// 指令塊 UI 元件
    /// </summary>
    public class ActionBlock : ContentControl
    {
        static ActionBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ActionBlock), 
                new FrameworkPropertyMetadata(typeof(ActionBlock)));
        }

        public static readonly DependencyProperty ActionTypeProperty =
            DependencyProperty.Register(nameof(ActionType), typeof(string), typeof(ActionBlock),
                new PropertyMetadata(string.Empty));

        public string ActionType
        {
            get => (string)GetValue(ActionTypeProperty);
            set => SetValue(ActionTypeProperty, value);
        }

        public static readonly DependencyProperty ActionTitleProperty =
            DependencyProperty.Register(nameof(ActionTitle), typeof(string), typeof(ActionBlock),
                new PropertyMetadata(string.Empty));

        public string ActionTitle
        {
            get => (string)GetValue(ActionTitleProperty);
            set => SetValue(ActionTitleProperty, value);
        }

        public static readonly DependencyProperty ActionColorProperty =
            DependencyProperty.Register(nameof(ActionColor), typeof(Brush), typeof(ActionBlock),
                new PropertyMetadata(Brushes.LightBlue));

        public Brush ActionColor
        {
            get => (Brush)GetValue(ActionColorProperty);
            set => SetValue(ActionColorProperty, value);
        }

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(ActionBlock),
                new PropertyMetadata(false));

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            
            if (e.ClickCount == 1)
            {
                IsSelected = true;
                DragDrop.DoDragDrop(this, this, DragDropEffects.Move);
            }
        }
    }
}
