using ACI318_19Library;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

public class RebarLayerViewModel : INotifyPropertyChanged
{
    public string BarSize { get; set; }
    public int Count { get; set; }
    public double Depth { get; set; }

    public double? LastTensionDepth { get; set; }

    public RebarLayerViewModel(string barSize, int count, double depth)
    {
        BarSize = barSize;
        Count = count;
        Depth = depth;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
