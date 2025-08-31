using System.ComponentModel;

public class RebarLayerViewModel : INotifyPropertyChanged
{
    public string BarSize { get; set; }
    public int Qty { get; set; }
    public double DepthFromTop { get; set; }

    public double? LastTensionDepth { get; set; }

    public RebarLayerViewModel(string barSize, int count, double depth)
    {
        BarSize = barSize;
        Qty = count;
        DepthFromTop = depth;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
