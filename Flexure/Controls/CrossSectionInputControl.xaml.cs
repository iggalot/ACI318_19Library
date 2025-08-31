using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace ACI318_19Library
{
    public partial class CrossSectionInputControl : UserControl
    {
        public CrossSectionViewModel ViewModel { get; private set; }

        public CrossSectionInputControl()
        {
            InitializeComponent();
            // create a default ViewModel if none provided
            ViewModel = new CrossSectionViewModel();
            DataContext = ViewModel;
        }
        public CrossSectionInputControl(CrossSectionViewModel vm)
        {
            InitializeComponent();
            ViewModel = vm;
            DataContext = ViewModel;

            ViewModel.TensionRebars.Add(new RebarLayerViewModel("#5", 2, 1.5));

            Debug.WriteLine("DataContext is null? " + (DataContext == null));
            Debug.WriteLine("TensionRebars count: " + ViewModel.TensionRebars.Count);
        }

        public CrossSection GetCrossSection() => ViewModel.ToCrossSection();

        private void BtnAddTension_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RebarLayerInputDialog(ViewModel.Catalog.RebarTable.Keys,
                                                   isTension: true,
                                                   sectionDepth: ViewModel.Depth,
                                                   cover: ViewModel.TensionCover);
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true)
            {
                ViewModel.AddTensionRebar(dialog.SelectedBarSize, dialog.Count, dialog.Depth);
                Debug.WriteLine($"TensionRebars count: {ViewModel.TensionRebars.Count}");
                TensionDataGrid.Items.Refresh();
            }
        }

        private void BtnAddComp_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RebarLayerInputDialog(ViewModel.Catalog.RebarTable.Keys,
                                                   isTension: false,
                                                   sectionDepth: ViewModel.Depth,
                                                   cover: ViewModel.CompressionCover);
            dialog.Owner = Window.GetWindow(this);

            if (dialog.ShowDialog() == true)
            {
                ViewModel.AddCompressionRebar(dialog.SelectedBarSize, dialog.Count, dialog.Depth);
            }
        }


        private void BtnRemoveTension_Click(object sender, RoutedEventArgs e)
        {
            if (TensionDataGrid.SelectedItem is RebarLayerViewModel layer)
                ViewModel.RemoveTensionRebar(layer);
        }

        private void BtnRemoveComp_Click(object sender, RoutedEventArgs e)
        {
            if (CompressionDataGrid.SelectedItem is RebarLayerViewModel layer)
                ViewModel.RemoveCompressionRebar(layer);
        }

    }
}
