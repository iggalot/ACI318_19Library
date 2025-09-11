using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace ACI318_19Library.Flexure.Controls
{
    public class DesignAllInputViewModel : INotifyPropertyChanged
    {
        private bool _sortByArea;
        public bool SortByArea
        {
            get => _sortByArea;
            set
            {
                if (_sortByArea != value)
                {
                    _sortByArea = value;
                    OnPropertyChanged(nameof(SortByArea));
                    ApplySorting();
                }
            }
        }

        private bool _filterWidth12;
        public bool FilterWidth12
        {
            get => _filterWidth12;
            set
            {
                if (_filterWidth12 != value)
                {
                    _filterWidth12 = value;
                    OnPropertyChanged(nameof(FilterWidth12));
                    // Refresh the view whenever the filter changes (guarded)
                    ValidDesignsView?.Refresh();
                    OnPropertyChanged(nameof(NumValidDesignsString));
                }
            }
        }

        // A filter for the list such that the maximum allowed rebar size is #7
        private bool _filterMaxBar7;
        public bool FilterMaxBar7
        {
            get => _filterMaxBar7;
            set
            {
                if (_filterMaxBar7 != value)
                {
                    _filterMaxBar7 = value;
                    OnPropertyChanged(nameof(FilterMaxBar7));
                    // Refresh the view whenever the filter changes (guarded)
                    ValidDesignsView?.Refresh();
                    OnPropertyChanged(nameof(NumValidDesignsString));
                }
            }
        }

        private double _designMomentMu_kipft = 50;
        public double DesignMomentMu_kipft
        {
            get => _designMomentMu_kipft;
            set
            {
                if (_designMomentMu_kipft != value)
                {
                    _designMomentMu_kipft = value;
                    OnPropertyChanged(nameof(DesignMomentMu_kipft));
                    OnPropertyChanged(nameof(DesignMomentMu_kipin));

                    // Run Update logic
                    Update();
                }
            }
        }

        private double _designShearVu_kip = 0;
        public double DesignShearVu_kip
        {
            get => _designMomentMu_kipft;
            set
            {
                if (_designShearVu_kip != value)
                {
                    _designShearVu_kip = value;
                    OnPropertyChanged(nameof(DesignShearVu_kip));
                    OnPropertyChanged(nameof(DesignShearVu_kip));

                    // Run Update logic
                    Update();
                }
            }
        }


        public double DesignMomentMu_kipin
        {
            get => _designMomentMu_kipft * 12.0;
            set
            {
                var newKipFt = value / 12.0;
                if (_designMomentMu_kipft != newKipFt)
                {
                    _designMomentMu_kipft = newKipFt;
                    OnPropertyChanged(nameof(DesignMomentMu_kipft));
                    OnPropertyChanged(nameof(DesignMomentMu_kipin));

                    // Run Update logic
                    Update();
                }
            }
        }

        // NUmShearLegs string uses the view so it reflects filtering
        public string NumValidDesignsString => $"{ValidDesignsView.Cast<object>().Count()} valid designs";

        public ICollectionView ValidDesignsView { get; private set; }

        private ObservableCollection<FlexuralDesignResultModel> _validDesigns;
        public ObservableCollection<FlexuralDesignResultModel> ValidDesigns
        {
            get => _validDesigns;
            set
            {
                if (_validDesigns != value)
                {
                    _validDesigns = value;
                    OnPropertyChanged(nameof(ValidDesigns));
                    // Re-create the view if someone replaces the collection at runtime
                    ValidDesignsView = CollectionViewSource.GetDefaultView(_validDesigns);
                    ValidDesignsView.Filter = FilterDesigns;
                    ApplySorting();
                    ValidDesignsView.Refresh();
                    OnPropertyChanged(nameof(NumValidDesignsString));
                }
            }
        }

        private FlexuralDesignResultModel _selectedDesign;
        public FlexuralDesignResultModel SelectedDesign
        {
            get => _selectedDesign;
            set
            {
                if (_selectedDesign != value)
                {
                    _selectedDesign = value;
                    OnPropertyChanged(nameof(SelectedDesign));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public DesignAllInputViewModel()
        {
            _validDesigns = new ObservableCollection<FlexuralDesignResultModel>();
            ValidDesignsView = CollectionViewSource.GetDefaultView(_validDesigns);

            // set the Filter ONCE
            ValidDesignsView.Filter = FilterDesigns;
        }

        public void Update()
        {
            // Repopulate ValidDesigns with newly computed designs
            ValidDesigns.Clear();

            FlexuralDesigner flex_design = new FlexuralDesigner();
            var newDesigns = new ObservableCollection<FlexuralDesignResultModel>(flex_design.DesignAllSectionsForMu(_designMomentMu_kipft));

            for (int i = 0; i < newDesigns.Count; i++)
            {
                var item = newDesigns[i]; // Copy to local variable
                ShearDesigner.ComputeShearCapacity(item.crossSection, ref item); // Modify by ref
                newDesigns[i] = item; // Assign back to collection
            }

            foreach (var d in newDesigns)
            {
                ValidDesigns.Add(d);
            }

            // Ensure filters and sorting are applied after repopulation
            ValidDesignsView?.Refresh();

            // Default selection: pick the first *visible* item from the view, not necessarily the first in the collection
            if (SelectedDesign == null)
            {
                SelectedDesign = ValidDesignsView.Cast<FlexuralDesignResultModel>().FirstOrDefault();
            }
            else
            {
                // If the currently selected item is filtered out, pick the first visible one
                var selectedStillVisible = ValidDesignsView.Cast<FlexuralDesignResultModel>().Any(d => d == SelectedDesign);
                if (!selectedStillVisible)
                    SelectedDesign = ValidDesignsView.Cast<FlexuralDesignResultModel>().FirstOrDefault();
            }

            OnPropertyChanged(nameof(NumValidDesignsString));
        }

        private bool FilterDesigns(object obj)
        {
            if (!(obj is FlexuralDesignResultModel design)) return false;

            // Width filter
            bool widthOk = !FilterWidth12 || design.crossSection.Width == 12;

            // Bar filter (combine tension + compression rebars, check any bar > #7)
            bool has_bar_greater7 = design.crossSection
                .TensionRebars.Concat(design.crossSection.CompressionRebars)
                .Any(d =>
                {
                    // Be defensive: ensure table contains the key
                    if (!RebarCatalog.RebarTable.ContainsKey(d.BarSize) || !RebarCatalog.RebarTable.ContainsKey("#7"))
                        return false;
                    return RebarCatalog.RebarTable[d.BarSize].Diameter >
                           RebarCatalog.RebarTable["#7"].Diameter;
                });

            bool barOk = !FilterMaxBar7 || !has_bar_greater7;

            return widthOk && barOk;
        }

        private void ApplySorting()
        {
            if (ValidDesignsView == null) return;

            using (ValidDesignsView.DeferRefresh())
            {
                ValidDesignsView.SortDescriptions.Clear();

                if (SortByArea)
                {
                    ValidDesignsView.SortDescriptions.Add(
                        new SortDescription(nameof(FlexuralDesignResultModel.AreaGross), ListSortDirection.Ascending));
                    // sort by your property; keep the property name you used earlier (AsT)
                    ValidDesignsView.SortDescriptions.Add(
                        new SortDescription(nameof(FlexuralDesignResultModel.AsT), ListSortDirection.Ascending));
                }
            }
        }
    }
}
