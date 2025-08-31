using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ACI318_19Library
{
    public partial class NumericUpDown : UserControl
    {
        private string _textValue = "0";
        public bool IsValid { get; private set; } = true;

        public NumericUpDown()
        {
            InitializeComponent();

            TextBoxValue.Text = _textValue;
            TextBoxValue.TextChanged += TextBoxValue_TextChanged;
        }

        // Routed event for parent controls
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

        // Value property
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(NumericUpDown),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        // Minimum property
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(NumericUpDown), new PropertyMetadata(double.MinValue));

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        // Maximum property
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
        public string ErrorMessage { get; private set; }

        private void BtnUp_Click(object sender, RoutedEventArgs e)
        {
            Value += Step;
            _textValue = Value.ToString();
            TextBoxValue.Text = _textValue;
            ClearError();
            RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
        }

        private void BtnDown_Click(object sender, RoutedEventArgs e)
        {
            Value -= Step;
            _textValue = Value.ToString();
            TextBoxValue.Text = _textValue;
            ClearError();
            RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
        }

        private void TextBoxValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            _textValue = TextBoxValue.Text;

            if (double.TryParse(_textValue, out double val))
            {
                if (val < Minimum)
                {
                    ShowError($"Minimum value is {Minimum}");
                }
                else if (val > Maximum)
                {
                    ShowError($"Maximum value is {Maximum}");
                }
                else
                {
                    Value = val;
                    ClearError();
                }
            }
            else
            {
                ShowError("Invalid number");
            }

            // Always notify parent controls
            RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
        }

        public void ShowError(string message)
        {
            TextBoxValue.Background = Brushes.LightPink; // entire textbox
            TextBoxValue.BorderBrush = Brushes.Red;      // optional
            ErrorMessage = message;
            ToolTipService.SetToolTip(TextBoxValue, message);
            IsValid = false;
        }

        public void ClearError()
        {
            TextBoxValue.ClearValue(TextBox.BackgroundProperty);
            TextBoxValue.ClearValue(TextBox.BorderBrushProperty);
            ErrorMessage = null;
            ToolTipService.SetToolTip(TextBoxValue, null);
            IsValid = true;
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (NumericUpDown)d;
            control._textValue = control.Value.ToString();
            control.TextBoxValue.Text = control._textValue;
            control.ClearError();
        }
    }
}
