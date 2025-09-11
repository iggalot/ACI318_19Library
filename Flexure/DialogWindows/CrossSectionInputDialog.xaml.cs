using System.Windows;

namespace ACI318_19Library
{
    /// <summary>
    /// Interaction logic for CrossSectionInputDialog.xaml
    /// </summary>
    public partial class CrossSectionInputDialog : Window
    {
        public ConcreteCrossSectionViewModel ViewModel { get; private set; }
        public CrossSectionInputDialog(ConcreteCrossSectionViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;

            var control = new ConcreteCrossSectionInputControl(ViewModel);
            spControl.Children.Add(control); 
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }
    }

}
