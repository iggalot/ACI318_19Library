using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace ACI318_19Library.Flexure.Controls
{
    public class DesignAllInputViewModel : INotifyPropertyChanged
    {
        private double _designMomentMu = 50;
        public double DesignMomentMu
        {
            get => _designMomentMu;
            set { 
                _designMomentMu = value; 
                OnPropertyChanged(nameof(DesignMomentMu));

                // Run Update logic
                Update();
            
            }
        }

        private ObservableCollection<DesignResultModel> _validDesigns = new ObservableCollection<DesignResultModel>();
        public ObservableCollection<DesignResultModel> ValidDesigns
        {
            get => _validDesigns;
            set { _validDesigns = value; OnPropertyChanged(nameof(ValidDesigns)); }
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

        private void Update()
        {
            ValidDesigns.Clear();

            FlexuralDesigner flex_design = new FlexuralDesigner();
            ValidDesigns = new ObservableCollection<DesignResultModel>(flex_design.DesignAllSections(_designMomentMu));
        }
    }
}
