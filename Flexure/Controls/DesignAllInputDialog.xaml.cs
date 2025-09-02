using ACI318_19Library.Flexure.Controls;
using System.Windows;

namespace ACI318_19Library
{
    /// <summary>
    /// Interaction logic for CrossSectionInputDialog.xaml
    /// </summary>
    public partial class DesignAllInputDialog : Window
    {
        public DesignAllInputViewModel ViewModel { get; set; }
        public DesignAllInputDialog(DesignAllInputViewModel vm)
        {
            InitializeComponent();

            ViewModel = vm;

            DataContext = ViewModel;

            this.Loaded += (s, e) => UpdateDefaultStyle();

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            ViewModel.Update();
            Update();
        }

        public double DesignMomentMu
        {
            get { return (double)GetValue(DesignMomentMuProperty); }
            set { SetValue(DesignMomentMuProperty, value); }
        }

        public static readonly DependencyProperty DesignMomentMuProperty =
            DependencyProperty.Register(nameof(DesignMomentMu), typeof(double),
                typeof(DesignAllInputDialog),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));


        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
                Update();
        }

        public void Update()
        {
            // Clear the design results control
            spResult.Children.Clear();

            // get our new section from the ViewModel and display the results
            DesignResultControl control = new DesignResultControl();
            control.Result = ViewModel.SelectedDesign;
            spResult.Children.Add(control);
        }
    }
}
