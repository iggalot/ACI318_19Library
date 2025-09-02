using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace ACI318_19Library.Flexure.Controls
{
    public class DesignAllInputViewModel : INotifyPropertyChanged
    {
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
                    // Refresh the view whenever the filter changes
                    ValidDesignsView.Refresh();
                    OnPropertyChanged(nameof(NumValidDesignsString));
                }
            }
        }


        private double _designMomentMu_kipft = 50;
        public double DesignMomentMu_kipft
        {
            get => _designMomentMu_kipft;
            set { 
                _designMomentMu_kipft = value; 
                OnPropertyChanged(nameof(DesignMomentMu_kipft));
                OnPropertyChanged(nameof(DesignMomentMu_kipin));

                // Run Update logic
                Update();
            
            }
        }

        public double DesignMomentMu_kipin
        {
            get => _designMomentMu_kipft * 12.0;
            set
            {
                _designMomentMu_kipft = value;
                OnPropertyChanged(nameof(DesignMomentMu_kipft));
                OnPropertyChanged(nameof(DesignMomentMu_kipin));

                // Run Update logic
                Update();

            }
        }

        //public string NumValidDesignsString { get => ValidDesigns.Count.ToString() + " valid designs"; }
        public string NumValidDesignsString { get => ValidDesignsView.Cast<object>().Count() + " valid designs"; }


        public ICollectionView ValidDesignsView { get; private set; }


        private ObservableCollection<DesignResultModel> _validDesigns;
        public ObservableCollection<DesignResultModel> ValidDesigns
        {
            get => _validDesigns;
            set { 
                _validDesigns = value; 
                OnPropertyChanged(nameof(ValidDesigns));
            }
        }

        private DesignResultModel _selectedDesign;
        public DesignResultModel SelectedDesign
        {
            get => _selectedDesign;
            set { _selectedDesign = value; OnPropertyChanged(nameof(SelectedDesign)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Update()
        {
            ValidDesigns.Clear();

            FlexuralDesigner flex_design = new FlexuralDesigner();
            var newDesigns = new ObservableCollection<DesignResultModel>(flex_design.DesignAllSections(_designMomentMu_kipft));

            foreach (var d in newDesigns)
            {
                ValidDesigns.Add(d);
            }

            // Default selection only if user hasn't chosen anything yet
            if (SelectedDesign == null && ValidDesigns.Count > 0)
                SelectedDesign = ValidDesigns[0];


            OnPropertyChanged(nameof(NumValidDesignsString));

        }

        public DesignAllInputViewModel()
        {
            _validDesigns = new ObservableCollection<DesignResultModel>();
            ValidDesignsView = CollectionViewSource.GetDefaultView(_validDesigns);
            ValidDesignsView.Filter = FilterDesigns;
        }

        private bool FilterDesigns(object obj)
        {
            if (!(obj is DesignResultModel design)) return false;

            if (FilterWidth12 && design.crossSection.Width != 12)
                return false;

            return true;
        }
    }
}
