using ACI318_19Library;
using ACI318_19Library.Flexure.Controls;
using System.Windows;

namespace ACI318Driver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnDesigner_Click(object sender, RoutedEventArgs e)
        {
            DesignAllInputViewModel viewModel = new DesignAllInputViewModel();
            DesignAllInputDialog dlg = new DesignAllInputDialog(viewModel);
            dlg.Owner = this;
            dlg.ShowDialog();
        }

        private void btnCrossSectionInput_Click(object sender, RoutedEventArgs e)
        {
            ConcreteCrossSectionViewModel viewModel = new ConcreteCrossSectionViewModel();
            CrossSectionInputDialog dlg = new CrossSectionInputDialog(viewModel);
            dlg.Owner = this;
            dlg.ShowDialog();
        }
    }
}