using System.Windows;
using System.Windows.Controls;

namespace ACI318_19Library
{
    /// <summary>
    /// Interaction logic for FlexureDesignResultControl.xaml
    /// </summary>
    public partial class ShearDesignResultControl : UserControl
    {
        public ShearDesignResultModel Result
        {
            get { return (ShearDesignResultModel)GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        public static readonly DependencyProperty ResultProperty =
            DependencyProperty.Register(nameof(Result),
                typeof(ShearDesignResultModel),
                typeof(ShearDesignResultControl),
                new PropertyMetadata(null, OnResultChanged));

        public ShearDesignResultControl()
        {
            InitializeComponent();


        }

        private static void OnResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ShearDesignResultControl)d;
            var model = e.NewValue as ShearDesignResultModel;
            if (model != null)
            {
                control.UpdateDisplay(); // method in your control to populate UI
            }
        }

        private void UpdateDisplay()
        {
            cnvShearStirrupSection.Children.Clear();

            if (Result != null)
            {
                // do stuff

                // draw figures
            }
        }
    }
}
