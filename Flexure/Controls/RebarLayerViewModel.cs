using System.ComponentModel;

public class RebarLayerViewModel : INotifyPropertyChanged
{
    private string _barSize;
    private int _qty;
    private double _depthFromTop;

    public string BarSize
    {
        get => _barSize;
        set
        {
            if (_barSize != value)
            {
                _barSize = value;
                OnPropertyChanged(nameof(BarSize));
            }
        }
    }

    public int Qty
    {
        get => _qty;
        set
        {
            if (_qty != value)
            {
                _qty = value;
                OnPropertyChanged(nameof(Qty));
            }
        }
    }

    public double DepthFromTop
    {
        get => _depthFromTop;
        set
        {
            if (_depthFromTop != value)
            {
                _depthFromTop = value;
                OnPropertyChanged(nameof(DepthFromTop));
            }
        }
    }

    public RebarLayerViewModel(string barSize, int count, double depth)
    {
        BarSize = barSize;
        Qty = count;
        DepthFromTop = depth;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
