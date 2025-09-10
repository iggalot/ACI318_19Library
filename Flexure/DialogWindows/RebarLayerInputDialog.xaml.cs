using System;
using System.Collections.Generic;
using System.Windows;

namespace ACI318_19Library
{
    public partial class RebarLayerInputDialog : Window
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

        public int Count
        {
            get
            {
                if (int.TryParse(CountTextBox.Text, out int c)) return c;
                return 0;
            }
            set => CountTextBox.Text = value.ToString();
        }

        public double Depth
        {
            get
            {
                if (double.TryParse(DepthTextBox.Text, out double d)) return d;
                return 0.0;
            }
            set => DepthTextBox.Text = value.ToString("F2");
        }

        // Parameterless ctor (allows you to set properties afterwards)
        public RebarLayerInputDialog()
        {
            InitializeComponent();
            _isTension = true; // default; caller can change by using the other ctor or SetDefaultDepth
            Count = 1;
        }

        // Overloaded ctor to create fully-initialized dialog
        public RebarLayerInputDialog(IEnumerable<string> barSizes, bool isTension, double sectionDepth, double cover) : this()
        {
            _isTension = isTension;
            BarSizes = barSizes;
            Count = 1;
            SetDefaultDepth(isTension, sectionDepth, cover);
        }

        /// <summary>
        /// Suggests a default depth based on whether the layer is tension or compression,
        /// and uses the remembered last depth (if any).
        /// </summary>
        public void SetDefaultDepth(bool isTension, double sectionDepth, double cover)
        {
            double suggested = isTension
                ? Math.Max(0.0, sectionDepth - cover - 0.5)   // tension bar centroid ~ d = depth - cover - 0.5"
                : Math.Max(0.0, cover + 0.5);                // compression bar centroid ~ cover + 0.5"

            double last = isTension ? _lastDepthTension : _lastDepthCompression;

            Depth = last > 0.0 ? last : suggested;
        }

        /// <summary>
        /// Save last depth for next time (call after dialog accepted).
        /// </summary>
        public void RememberDepth(bool isTension)
        {
            var d = Depth;
            if (d <= 0) return;
            if (isTension) _lastDepthTension = d; else _lastDepthCompression = d;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            // Basic validation
            if (BarSizeComboBox.SelectedItem == null)
            {
                MessageBox.Show(this, "Please select a bar size.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(CountTextBox.Text, out int cnt) || cnt <= 0)
            {
                MessageBox.Show(this, "Qty must be a positive integer.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(DepthTextBox.Text, out double depth) || depth <= 0.0)
            {
                MessageBox.Show(this, "DepthFromTop must be a positive number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save parsed values into public properties via setters (redundant but explicit)
            Count = cnt;
            Depth = depth;

            // record last depth for later calls
            RememberDepth(_isTension);

            DialogResult = true; // close with true
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
