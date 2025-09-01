using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ACI318_19Library
{
    public class CrossSectionViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private double _width = 8;
        private double _depth = 12;
        private double _tensionCover = 1.5;
        private double _compressionCover = 1.5;
        private double _sideCover = 1.5;
        private double _clearSpacing = 2.0;
        private double _fck = 4000;  // psi
        private double _fy = 60000;  // psi

        public double Width { get => _width; set { _width = value; OnPropertyChanged(nameof(Width)); } }
        public double Depth { get => _depth; set { _depth = value; OnPropertyChanged(nameof(Depth)); } }
        public double TensionCover { get => _tensionCover; set { _tensionCover = value; OnPropertyChanged(nameof(TensionCover)); } }
        public double CompressionCover { get => _compressionCover; set { _compressionCover = value; OnPropertyChanged(nameof(CompressionCover)); } }
        public double SideCover { get => _sideCover; set { _sideCover = value; OnPropertyChanged(nameof(SideCover)); } }
        public double ClearSpacing { get => _clearSpacing; set { _clearSpacing = value; OnPropertyChanged(nameof(ClearSpacing)); } }
        public double Fck { get => _fck; set { _fck = value; OnPropertyChanged(nameof(Fck)); } }
        public double Fy { get => _fy; set { _fy = value; OnPropertyChanged(nameof(Fy)); } }

        public CrossSection ToCrossSection()
        {
            RebarCatalog catalog = new RebarCatalog();
            ObservableCollection<RebarLayer> tension_rebars = new ObservableCollection<RebarLayer>();
            ObservableCollection<RebarLayer> compression_rebars = new ObservableCollection<RebarLayer>();
            foreach (RebarLayerViewModel layer in TensionRebars)
            {
                RebarLayer temp = new RebarLayer(layer.BarSize, layer.Qty, catalog.RebarTable[layer.BarSize], layer.DepthFromTop);
                tension_rebars.Add(temp);
            }

            foreach (RebarLayerViewModel layer in CompressionRebars)
            {
                RebarLayer temp = new RebarLayer(layer.BarSize, layer.Qty, catalog.RebarTable[layer.BarSize], layer.DepthFromTop);
                compression_rebars.Add(temp);
            }

            return new CrossSection()
            {
                Width = Width,
                Height = Depth,
                TensionCover = TensionCover,
                CompressionCover = CompressionCover,
                SideCover = SideCover,
                ClearSpacing = ClearSpacing,
                Fck_psi = Fck,
                Fy_psi = Fy,
                TensionRebars = tension_rebars,
                CompressionRebars = compression_rebars
            };
        }

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(Width):
                    case nameof(Depth):
                    case nameof(TensionCover):
                    case nameof(CompressionCover):
                    case nameof(SideCover):
                    case nameof(ClearSpacing):
                    case nameof(Fck):
                    case nameof(Fy):
                        if (GetType().GetProperty(columnName).GetValue(this) is double value)
                        {
                            if (value <= 0) return "Value must be positive";
                        }
                        break;
                }
                return null;
            }
        }


        // Rebar layer lists
        public ObservableCollection<RebarLayerViewModel> TensionRebars { get; set; } = new ObservableCollection<RebarLayerViewModel>();
        public ObservableCollection<RebarLayerViewModel> CompressionRebars { get; set; } = new ObservableCollection<RebarLayerViewModel>();

        // Example rebar catalog (in a real app, pass it in)
        public RebarCatalog Catalog { get; set; } = new RebarCatalog();



        // Add tension layer
        public void AddTensionRebar(string barSize, int count, double depth)
        {
            if (!Catalog.RebarTable.ContainsKey(barSize)) return;
            TensionRebars.Add(new RebarLayerViewModel(barSize, count, depth));
        }

        // Add compression layer
        public void AddCompressionRebar(string barSize, int count, double depth)
        {
            if (!Catalog.RebarTable.ContainsKey(barSize)) return;
            CompressionRebars.Add(new RebarLayerViewModel(barSize, count, depth));
        }

        // Remove selected layers (you can bind SelectedItem from DataGrid)
        public void RemoveTensionRebar(RebarLayerViewModel layer) => TensionRebars.Remove(layer);
        public void RemoveCompressionRebar(RebarLayerViewModel layer) => CompressionRebars.Remove(layer);

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
