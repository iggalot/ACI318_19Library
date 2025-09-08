using ACI318_19Library.Flexure.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ACI318_19Library
{
    /// <summary>
    /// Interaction logic for CrossSectionInputDialog.xaml
    /// </summary>
    public partial class DesignAllInputDialog : Window
    {
        private bool _initialized = false;
        public DesignAllInputViewModel ViewModel { get; set; }
        public DesignAllInputDialog(DesignAllInputViewModel vm)
        {
            InitializeComponent();

            ViewModel = vm;


            DataContext = ViewModel;

            this.Loaded += (s, e) =>
            {
                _initialized = true;
                // Clear seelction so nothing is auto-selected  
                listValidDesigns.SelectedIndex = -1;

                UpdateDefaultStyle();
            };

            //// use this event hook to trigger an event of a slecrtion change in a list box
            //// -- since we are launching the cross section designer on the selection now, we don't need it.
            //// -- Leaving htis commented as a reminder.
            //ViewModel.PropertyChanged += ViewModel_PropertyChanged;

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

 //           CrossSectionViewModel vm = new CrossSectionViewModel(ViewModel.SelectedDesign.crossSection);
 //           CrossSectionInputDialog dlg = new CrossSectionInputDialog(vm);
 ////           dlg.Owner = this;
 //           dlg.ShowDialog();
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

        private void listValidDesigns_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_initialized == false) return;  // break on the initial run

            if (e.AddedItems.Count > 0 && listValidDesigns.SelectedIndex >= 0)
            {
                var crossSectionVM = new CrossSectionViewModel(ViewModel.SelectedDesign.crossSection);

                var dlg = new CrossSectionInputDialog(crossSectionVM)
                {
                    Owner = this
                };

                dlg.ShowDialog();

                // Clear selection so they can re-click the same item if needed
                listValidDesigns.SelectedIndex = -1;
            }

            Update();
        }

        private void ListBoxItem_MouseEnter(object sender, MouseEventArgs e)
        {
            var item = (sender as ListBoxItem)?.DataContext as DesignResultModel;
            if (item != null)
            {
                // Clear the results panel
                spResult.Children.Clear();

                // Build and show the control
                var control = new DesignResultControl
                {
                    Result = item
                };
                spResult.Children.Add(control);
            }
        }

    }
}
