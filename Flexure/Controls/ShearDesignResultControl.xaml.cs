using System.Windows;
using System.Windows.Controls;

namespace ACI318_19Library
{
    /// <summary>
    /// Interaction logic for FlexureDesignResultControl.xaml
    /// </summary>
    public partial class ShearDesignResultControl : UserControl
    {
        public FlexuralDesignResultModel Result
        {
            get { return (FlexuralDesignResultModel)GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        public static readonly DependencyProperty ResultProperty =
            DependencyProperty.Register(nameof(Result),
                typeof(FlexuralDesignResultModel),
                typeof(ShearDesignResultControl),
                new PropertyMetadata(null, OnResultChanged));

        public ShearDesignResultControl()
        {
            InitializeComponent();


        }

        private static void OnResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ShearDesignResultControl)d;
            var model = e.NewValue as FlexuralDesignResultModel;
            if (model != null)
            {
                control.UpdateDisplay(); // method in your control to populate UI
            }
        }

        private void UpdateDisplay()
        {
            //cnvShearStirrupSection.Children.Clear();

            if (Result != null)
            {
                // do stuff

                // draw figures
            }
        }
    }
}
