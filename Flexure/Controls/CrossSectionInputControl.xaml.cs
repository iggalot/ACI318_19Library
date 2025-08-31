using System.Collections.ObjectModel;
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

            this.Loaded += (s, e) => Update();
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
            Update();
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
            Update();
        }


        private void BtnRemoveTension_Click(object sender, RoutedEventArgs e)
        {
            if (TensionDataGrid.SelectedItem is RebarLayerViewModel layer)
                ViewModel.RemoveTensionRebar(layer);

            Update();
        }

        private void BtnRemoveComp_Click(object sender, RoutedEventArgs e)
        {
            if (CompressionDataGrid.SelectedItem is RebarLayerViewModel layer)
                ViewModel.RemoveCompressionRebar(layer);

            Update();
        }

        public void Update()
        {
            RebarCatalog catalog = new RebarCatalog();
            ObservableCollection<RebarLayer> tension_rebars = new ObservableCollection<RebarLayer>();
            ObservableCollection<RebarLayer> compression_rebars = new ObservableCollection<RebarLayer>();
            foreach (RebarLayerViewModel layer in ViewModel.TensionRebars)
            {
                RebarLayer temp = new RebarLayer(layer.BarSize, layer.Qty, catalog.RebarTable[layer.BarSize], layer.DepthFromTop);
                tension_rebars.Add(temp);
            }

            foreach (RebarLayerViewModel layer in ViewModel.CompressionRebars)
            {
                RebarLayer temp = new RebarLayer(layer.BarSize, layer.Qty, catalog.RebarTable[layer.BarSize], layer.DepthFromTop);
                compression_rebars.Add(temp);
            }

            CrossSection section = new CrossSection()
            {
                Width = ViewModel.Width,
                Depth = ViewModel.Depth,
                TensionCover = ViewModel.TensionCover,
                CompressionCover = ViewModel.CompressionCover,
                SideCover = ViewModel.SideCover,
                ClearSpacing = ViewModel.ClearSpacing,
                Fck_psi = ViewModel.Fck,
                Fy_psi = ViewModel.Fy,
                TensionRebars = tension_rebars,
                CompressionRebars = compression_rebars
            };

            // Draw the cross section
            //cnvCrossSection.Children.Clear();
            //ACIDrawingHelpers.DrawCrossSection(cnvCrossSection, section);

            // Show the design results
            spResult.Children.Clear();

            // Perform a moment calculation
            if (tension_rebars.Count == 0) return;

            DesignResultModel design = FlexuralDesigner.ComputeFlexuralStrength(section);
            DesignResultControl control = new DesignResultControl();
            control.Result = design;
            spResult.Children.Add(control);





        }

    }
}
