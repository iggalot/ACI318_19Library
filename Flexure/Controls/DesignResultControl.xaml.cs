using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

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
            control.UpdateDisplay();
            //if (d is DesignResultControl control && e.NewValue is DesignResultModel model)
            //{
            //    control.DataContext = model;
            //    control.DrawCrossSection(model.crossSection);
            //}
        }

        private void UpdateDisplay()
        {
            // Bind DataContext so XAML TextBlocks work
            DataContext = Result;

            // Also update canvas drawing
            cnvSection.Children.Clear();
            ACIDrawingHelpers.DrawCrossSection(cnvSection, Result.crossSection);
        }
    }
}
