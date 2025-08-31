using System.Windows;

namespace ACI318_19Library
{
    /// <summary>
    /// Interaction logic for CrossSectionInputDialog.xaml
    /// </summary>
    public partial class CrossSectionInputDialog : Window
    {
        public CrossSectionViewModel ViewModel { get; private set; }
        public CrossSectionInputDialog(CrossSectionViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;

            var control = new CrossSectionInputControl(ViewModel);
            spControl.Children.Add(control); 
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }
    }

}
