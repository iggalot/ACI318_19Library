using ACI318_19Library;
using System.ComponentModel;

public class StirrupRebarLayerViewModel : INotifyPropertyChanged
{
    private string _barSize;
    private int _num_shear_legs;
    private double _spacing;
    private double _start_pos;
    private double _end_pos;

    public string BarSize
    {
        get => _barSize;
        set
        {
            if (_barSize != value)
            {
                _barSize = value;
                UpdateUI();
            }
        }
    }

    public int NumShearLegs
    {
        get => _num_shear_legs;
        set
        {
            if (_num_shear_legs != value)
            {
                _num_shear_legs = value;
                UpdateUI();
            }
        }
    }

    public double Spacing
    {
        get => _spacing;
        set
        {
            if (_spacing != value)
            {
                _spacing = value;
                UpdateUI();
            }
        }
    }

    public double StartPos
    {
        get => _start_pos;
        set
        {
            if (_start_pos != value)
            {
                _start_pos = value;
                UpdateUI();
            }
        }
    }

    public double EndPos
    {
        get => _end_pos;
        set
        {
            if (_end_pos != value)
            {
                _end_pos = value;
                UpdateUI();
            }
        }
    }

    public double Av_over_S
    {
        get => RebarCatalog.RebarTable[BarSize].Area * NumShearLegs / Spacing;
    }

    public void UpdateUI()
    {
        OnPropertyChanged(nameof(BarSize));
        OnPropertyChanged(nameof(NumShearLegs));
        OnPropertyChanged(nameof(NumShearLegs));
        OnPropertyChanged(nameof(Spacing));
        OnPropertyChanged(nameof(StartPos));
        OnPropertyChanged(nameof(EndPos));
        OnPropertyChanged(nameof(Av_over_S));
    }

    public StirrupRebarLayerViewModel(string barSize, int count, double spacing, double startPos, double endPos)
    {
        BarSize = barSize;
        NumShearLegs = count;
        Spacing = spacing;
        StartPos = startPos;
        EndPos = endPos;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
