using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ACI318_19Library
{
    public partial class NumericUpDown : UserControl
    {
        public NumericUpDown()
        {
            InitializeComponent();
            TextBoxValue.LostFocus += TextBoxValue_LostFocus;
        }

        public static readonly RoutedEvent ValueChangedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(ValueChanged),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(NumericUpDown));

        public event RoutedEventHandler ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }

        // Value
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(NumericUpDown),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        // Minimum
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(NumericUpDown), new PropertyMetadata(double.MinValue));

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        // Maximum
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(NumericUpDown), new PropertyMetadata(double.MaxValue));

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        // Step
        public double Step { get; set; } = 1.0;

        // Error message for tooltip
        public string ErrorMessage { get; set; }

        private void BtnUp_Click(object sender, RoutedEventArgs e)
        {
            Value += Step;
            ValidateValue();
        }

        private void BtnDown_Click(object sender, RoutedEventArgs e)
        {
            Value -= Step;
            ValidateValue();
        }

        private void TextBoxValue_LostFocus(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(TextBoxValue.Text, out double val))
            {
                Value = val;
            }
            ValidateValue();
        }

        private void ValidateValue()
        {
            if (Value < Minimum)
            {
                Value = Minimum;
                ShowError($"Minimum value is {Minimum}");
            }
            else if (Value > Maximum)
            {
                Value = Maximum;
                ShowError($"Maximum value is {Maximum}");
            }
            else
            {
                ClearError();
            }
        }

        public void ShowError(string message)
        {
            TextBoxValue.BorderBrush = Brushes.Red;
            ErrorMessage = message;
            ToolTipService.SetToolTip(TextBoxValue, message);
        }

        public void ClearError()
        {
            TextBoxValue.ClearValue(Border.BorderBrushProperty);
            ErrorMessage = null;
            ToolTipService.SetToolTip(TextBoxValue, null);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (NumericUpDown)d;
            control.ValidateValue();
            // Raise the event for parent controls
            control.RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
        }

        private void TextBoxValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(TextBoxValue.Text, out double val))
            {
                Value = val;
                ClearError();
            }
            else
            {
                ShowError("Invalid number");
            }

            // Always notify parent
            RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
        }

    }
}
