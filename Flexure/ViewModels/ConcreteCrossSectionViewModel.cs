using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace ACI318_19Library
{
    public class ConcreteCrossSectionViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private double _width = 8;
        private double _height = 12;
        private double _tensionCover = 1.5;
        private double _compressionCover = 1.5;
        private double _sideCover = 1.5;
        private double _clearSpacing = 2.0;
        private double _fck = 4000;  // psi
        private double _fy = 60000;  // psi

        public bool IsValid { get; set; } = true;
        public double Width { get => _width; set { _width = value; OnPropertyChanged(nameof(Width)); } }
        public double Height
        {
            get => _height;
            set
            {
                // hold the rebar locations so that the bottom dimension is constant in the cross section.
                // i.e. if we change the depth of the section, the bottom bars adjust accordingly.
                foreach (var item in TensionRebars)
                {
                    var depth = item.DepthFromTop;
                    var current_height = Height;
                    var bottom_dist = current_height - depth;
                    item.DepthFromTop = value - bottom_dist;
                }
                _height = value; OnPropertyChanged(nameof(Height));
            }
        }
        public double TensionCover { get => _tensionCover; set { _tensionCover = value; OnPropertyChanged(nameof(TensionCover)); } }
        public double CompressionCover { get => _compressionCover; set { _compressionCover = value; OnPropertyChanged(nameof(CompressionCover)); } }
        public double SideCover { get => _sideCover; set { _sideCover = value; OnPropertyChanged(nameof(SideCover)); } }
        public double ClearSpacing { get => _clearSpacing; set { _clearSpacing = value; OnPropertyChanged(nameof(ClearSpacing)); } }
        public double Fck_psi { get => _fck; set { _fck = value; OnPropertyChanged(nameof(Fck_psi)); } }
        public double Fy_psi { get => _fy; set { _fy = value; OnPropertyChanged(nameof(Fy_psi)); } }

        // Rebar layer lists
        public ObservableCollection<RebarLayerViewModel> TensionRebars { get; set; } = new ObservableCollection<RebarLayerViewModel>();
        public ObservableCollection<RebarLayerViewModel> CompressionRebars { get; set; } = new ObservableCollection<RebarLayerViewModel>();
        public ObservableCollection<StirrupRebarLayerViewModel> StirrupRebars { get; set; } = new ObservableCollection<StirrupRebarLayerViewModel>();


        public ConcreteCrossSectionViewModel()
        {

        }

        public ConcreteCrossSectionViewModel(ConcreteCrossSection section)
        {
            Width = section.Width;
            Height = section.Height;
            TensionCover = section.TensionCover;
            SideCover = section.SideCover;
            ClearSpacing = section.ClearSpacing;
            CompressionCover = section.CompressionCover;
            Fck_psi = section.Fck_psi;

            foreach (var layer in section.TensionRebars)
            {
                TensionRebars.Add(new RebarLayerViewModel(layer.BarSize, layer.Qty, layer.DepthFromTop));
            }

            foreach (var layer in section.CompressionRebars)
            {
                CompressionRebars.Add(new RebarLayerViewModel(layer.BarSize, layer.Qty, layer.DepthFromTop));
            }

            foreach (var layer in section.StirrupRebars)
            {
                StirrupRebars.Add(new StirrupRebarLayerViewModel(layer.BarSize, layer.NumShearLegs, layer.Spacing, layer.StartPos, layer.EndPos));
            }

        }
        public ConcreteCrossSection ToCrossSection()
        {
            ObservableCollection<RebarLayer> tension_rebars = new ObservableCollection<RebarLayer>();
            ObservableCollection<RebarLayer> compression_rebars = new ObservableCollection<RebarLayer>();
            ObservableCollection<RebarStirrupLayer> stirrup_rebars = new ObservableCollection<RebarStirrupLayer>();

            foreach (RebarLayerViewModel layer in TensionRebars)
            {
                try
                {
                    RebarLayer temp = new RebarLayer(layer.BarSize, layer.Qty, RebarCatalog.RebarTable[layer.BarSize], layer.DepthFromTop);
                    tension_rebars.Add(temp);
                }
                catch
                {
                    MessageBox.Show("Error in adding tension rebar data value");
                    return null;
                }

            }

            foreach (RebarLayerViewModel layer in CompressionRebars)
            {
                try
                {
                    RebarLayer temp = new RebarLayer(layer.BarSize, layer.Qty, RebarCatalog.RebarTable[layer.BarSize], layer.DepthFromTop);
                    compression_rebars.Add(temp);
                }
                catch
                {
                    MessageBox.Show("Error in adding compression rebar data value");
                    return null;
                }
            }

            foreach (StirrupRebarLayerViewModel layer in StirrupRebars)
            {
                try
                {
                    RebarStirrupLayer temp = new RebarStirrupLayer(layer.BarSize, layer.NumShearLegs, RebarCatalog.RebarTable[layer.BarSize], layer.Spacing, layer.StartPos, layer.EndPos);
                    stirrup_rebars.Add(temp);
                }
                catch
                {
                    MessageBox.Show("Error in adding compression rebar data value");
                    return null;
                }
            }

            return new ConcreteCrossSection()
            {
                Width = Width,
                Height = Height,
                TensionCover = TensionCover,
                CompressionCover = CompressionCover,
                SideCover = SideCover,
                ClearSpacing = ClearSpacing,
                Fck_psi = Fck_psi,
                Fy_psi = Fy_psi,
                TensionRebars = tension_rebars,
                CompressionRebars = compression_rebars,
                StirrupRebars = stirrup_rebars,
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
                    case nameof(Height):
                    case nameof(TensionCover):
                    case nameof(CompressionCover):
                    case nameof(SideCover):
                    case nameof(ClearSpacing):
                    case nameof(Fck_psi):
                    case nameof(Fy_psi):
                        if (GetType().GetProperty(columnName).GetValue(this) is double value)
                        {
                            if (value <= 0) return "Value must be positive";
                        }
                        break;
                }
                return null;
            }
        }



        // Example rebar catalog (in a real app, pass it in)

        // Add tension layer
        public void AddTensionRebar(string barSize, int count, double depth)
        {
            if (!RebarCatalog.RebarTable.ContainsKey(barSize)) return;
            TensionRebars.Add(new RebarLayerViewModel(barSize, count, depth));
        }

        // Add compression layer
        public void AddCompressionRebar(string barSize, int count, double depth)
        {
            if (!RebarCatalog.RebarTable.ContainsKey(barSize)) return;
            CompressionRebars.Add(new RebarLayerViewModel(barSize, count, depth));
        }

        // Add stirrup layer
        public void AddStirrupRebar(string barSize, int num_leg, double spacing, double start, double end)
        {
            if (!RebarCatalog.RebarTable.ContainsKey(barSize)) return;
            StirrupRebars.Add(new StirrupRebarLayerViewModel(barSize, num_leg, spacing, start, end));
        }

        // Remove selected layers (you can bind SelectedItem from DataGrid)
        public void RemoveTensionRebar(RebarLayerViewModel layer) => TensionRebars.Remove(layer);
        public void RemoveCompressionRebar(RebarLayerViewModel layer) => CompressionRebars.Remove(layer);

        public void RemoveStirrupRebar(StirrupRebarLayerViewModel layer) => StirrupRebars.Remove(layer);

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
