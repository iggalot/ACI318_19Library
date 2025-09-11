using System.Collections.Generic;
using System.Windows;

namespace ACI318_19Library
{
    public partial class StirrupRebarLayerInputDialog : Window
    {
        // remembers last depths across dialog instances
        private static double _lastDepthTension = 0.0;
        private static double _lastDepthCompression = 0.0;

        private readonly bool _isTension;

        // Public accessors used by caller
        public IEnumerable<string> BarSizes
        {
            get => (IEnumerable<string>)BarSizeComboBox.ItemsSource;
            set
            {
                BarSizeComboBox.ItemsSource = value;
                if (BarSizeComboBox.Items.Count > 0)
                    BarSizeComboBox.SelectedIndex = 0;
            }
        }

        public string SelectedBarSize => BarSizeComboBox.SelectedItem?.ToString();

        public int NumShearLegs
        {
            get
            {
                if (int.TryParse(NumShearLegsTextBox.Text, out int c)) return c;
                return 0;
            }
            set => NumShearLegsTextBox.Text = value.ToString();
        }

        public double Spacing
        {
            get
            {
                if (double.TryParse(SpacingTextBox.Text, out double d)) return d;
                return 0.0;
            }
            set => SpacingTextBox.Text = value.ToString("F2");
        }


        public double StartPos
        {
            get
            {
                if (double.TryParse(StartTextBox.Text, out double d)) return d;
                return 0.0;
            }
            set => StartTextBox.Text = value.ToString("F2");
        }

        public double EndPos
        {
            get
            {
                if (double.TryParse(EndTextBox.Text, out double d)) return d;
                return 0.0;
            }
            set => EndTextBox.Text = value.ToString("F2");
        }

        // Parameterless ctor (allows you to set properties afterwards)
        public StirrupRebarLayerInputDialog()
        {
            InitializeComponent();

            BarSizes = new List<string>() { "#3", "#4", "#5", "#6", "#7", "#8", "#9", "#10", "#11" };
            NumShearLegs = 2;
            Spacing = 6;
            StartPos = 0;
            EndPos = 1000;


        }

        // Overloaded ctor to create fully-initialized dialog
        public StirrupRebarLayerInputDialog(IEnumerable<string> barSizes, int num_shear_legs, double spacing=6, double start=0, double end=1000) : this()
        {
            InitializeComponent();

            BarSizes = barSizes;
            NumShearLegs = 2;
            Spacing = spacing;
            StartPos = start;
            EndPos = end;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            // Basic validation
            if (BarSizeComboBox.SelectedItem == null)
            {
                MessageBox.Show(this, "Please select a bar size.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(NumShearLegsTextBox.Text, out int cnt) || cnt <= 0)
            {
                MessageBox.Show(this, "NumShearLegs must be a positive integer.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(SpacingTextBox.Text, out double spacing) || spacing <= 0.0)
            {
                MessageBox.Show(this, "Spacing must be a positive number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(StartTextBox.Text, out double start) || start < 0.0)
            {
                MessageBox.Show(this, "Start position must be greater than or equal to zero..", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(EndTextBox.Text, out double end) || end <= start)
            {
                MessageBox.Show(this, "End position must be a positive number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save parsed values into public properties via setters (redundant but explicit)
            NumShearLegs = cnt;
            Spacing = spacing;
            StartPos = start;
            EndPos = end;


            DialogResult = true; // close with true
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
