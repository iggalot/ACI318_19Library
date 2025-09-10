using System.Windows;
using System.Windows.Controls;

namespace ACI318_19Library
{
    /// <summary>
    /// Interaction logic for FlexureDesignResultControl.xaml
    /// </summary>
<<<<<<<< Updated upstream:Flexure/Controls/FlexureDesignResultControl.xaml.cs
    public partial class FlexureDesignResultControl : UserControl
========
    public partial class FlexuralDesignResultControl : UserControl
>>>>>>>> Stashed changes:Flexure/Controls/FlexuralDesignResultControl.xaml.cs
    {
        public FlexuralDesignResultModel Result
        {
            get { return (FlexuralDesignResultModel)GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        public static readonly DependencyProperty ResultProperty =
            DependencyProperty.Register(nameof(Result),
<<<<<<<< Updated upstream:Flexure/Controls/FlexureDesignResultControl.xaml.cs
                typeof(FlexuralDesignResultModel),
                typeof(FlexureDesignResultControl),
                new PropertyMetadata(null, OnResultChanged));

        public FlexureDesignResultControl()
========
                typeof(DesignResultModel),
                typeof(FlexuralDesignResultControl),
                new PropertyMetadata(null, OnResultChanged));

        public FlexuralDesignResultControl()
>>>>>>>> Stashed changes:Flexure/Controls/FlexuralDesignResultControl.xaml.cs
        {
            InitializeComponent();


        }

        private static void OnResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
<<<<<<<< Updated upstream:Flexure/Controls/FlexureDesignResultControl.xaml.cs
            var control = (FlexureDesignResultControl)d;
            var model = e.NewValue as FlexuralDesignResultModel;
========
            var control = (FlexuralDesignResultControl)d;
            var model = e.NewValue as DesignResultModel;
>>>>>>>> Stashed changes:Flexure/Controls/FlexuralDesignResultControl.xaml.cs
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
                ACIDrawingHelpers.DrawFlexuralStrainDiagram(cnvStrainDiagram, Result);
            }
        }
    }
}
