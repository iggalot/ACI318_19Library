using ACI318_19Library;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class RebarLayerViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
{
    private int _qty;
    public int Qty
    {
        get { return _qty; }
        set
        {
            if (_qty != value)
            {
                _qty = value;
                OnPropertyChanged();
                Validate();
            }
        }
    }

    private string _barSize;
    public string BarSize
    {
        get { return _barSize; }
        set
        {
            if (_barSize != value)
            {
                _barSize = value;
                OnPropertyChanged();
                Validate();
            }
        }
    }

    private double _depthFromTop;
    public double DepthFromTop
    {
        get { return _depthFromTop; }
        set
        {
            if (_depthFromTop != value)
            {
                _depthFromTop = value;
                OnPropertyChanged();
                Validate();
            }
        }
    }

    // Errors dictionary
    private readonly Dictionary<string, List<string>> _errors = new Dictionary<string, List<string>>();

    private void Validate()
    {
        _errors.Clear();

        if (Qty <= 0)
            _errors["Qty"] = new List<string> { "Quantity must be greater than 0." };

        if (DepthFromTop <= 0)
            _errors["DepthFromTop"] = new List<string> { "Depth must be greater than 0." };

        if (string.IsNullOrEmpty(BarSize) || !RebarCatalog.RebarTable.ContainsKey(BarSize))
            _errors["BarSize"] = new List<string> { "Invalid bar size." };

        if (ErrorsChanged != null)
            ErrorsChanged(this, new DataErrorsChangedEventArgs(null));
    }

    public bool HasErrors
    {
        get { return _errors.Count > 0; }
    }

    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

    public IEnumerable GetErrors(string propertyName)
    {
        if (!string.IsNullOrEmpty(propertyName) && _errors.ContainsKey(propertyName))
            return _errors[propertyName];
        return null;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        if (PropertyChanged != null)
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="barSize"></param>
    /// <param name="count"></param>
    /// <param name="depth"></param>
    public RebarLayerViewModel(string barSize, int count, double depth)
    {
        BarSize = barSize;
        Qty = count;
        DepthFromTop = depth;
    }
}
