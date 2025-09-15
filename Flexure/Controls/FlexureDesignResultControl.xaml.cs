using System.Windows;
using System.Windows.Controls;

namespace ACI318_19Library
{
    /// <summary>
    /// Interaction logic for FlexureDesignResultControl.xaml
    /// </summary>
    public partial class FlexureDesignResultControl : UserControl
    {
        public FlexuralDesignResultModel Result
        {
            get { return (FlexuralDesignResultModel)GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        public static readonly DependencyProperty ResultProperty =
            DependencyProperty.Register(nameof(Result),
                typeof(FlexuralDesignResultModel),
                typeof(FlexureDesignResultControl),
                new PropertyMetadata(null, OnResultChanged));

        public FlexureDesignResultControl()
        {
            InitializeComponent();


        }

        private static void OnResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FlexureDesignResultControl)d;
            var model = e.NewValue as FlexuralDesignResultModel;
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
                if(Result.crossSection != null)
                {
                    ACIDrawingHelpers.DrawCrossSection(cnvSection, Result);
                    ACIDrawingHelpers.DrawFlexuralStrainDiagram(cnvStrainDiagram, Result);
                }
            }
        }
    }
}
