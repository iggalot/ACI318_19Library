using System.Windows;
using System.Windows.Controls;

namespace ACI318_19Library
{
    /// <summary>
    /// Interaction logic for DesignResultControl.xaml
    /// </summary>
    public partial class DesignResultControl : UserControl
    {
        public DesignResultModel Result
        {
            get { return (DesignResultModel)GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        public static readonly DependencyProperty ResultProperty =
            DependencyProperty.Register(nameof(Result),
                typeof(DesignResultModel),
                typeof(DesignResultControl),
                new PropertyMetadata(null, OnResultChanged));

        public DesignResultControl()
        {
            InitializeComponent();


        }

        private static void OnResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DesignResultControl)d;
            var model = e.NewValue as DesignResultModel;
            if (model != null)
            {
                control.UpdateDisplay(); // method in your control to populate UI
            }
        }

        private void UpdateDisplay()
        {
            cnvSection.Children.Clear();

            if (Result != null)
            {
                ACIDrawingHelpers.DrawCrossSection(cnvSection, Result);
                ACIDrawingHelpers.DrawStrainDiagram(cnvStrainDiagram, Result);
            }
        }
    }
}
