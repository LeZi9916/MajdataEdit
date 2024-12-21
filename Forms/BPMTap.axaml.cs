using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;

namespace MajdataEdit;

public partial class BPMTap : Window
{
    List<double> _bpms = new();
    DateTime _lastTime = DateTime.MinValue;
    public BPMTap()
    {
        InitializeComponent();
    }
    void Update()
    {
        if (_lastTime != DateTime.MinValue)
        {
            var delta = (DateTime.Now - _lastTime).TotalSeconds;
            _bpms.Add(60d / delta);
        }

        _lastTime = DateTime.Now;
        double sum = 0;
        if (_bpms.Count <= 0) return;
        if (_bpms.Count > 20) _bpms.RemoveAt(0);
        foreach (var bpm in _bpms) sum += bpm;
        var avg = sum / _bpms.Count;

        TapBtn.Content = string.Format("{0:N1}", avg);
    }
    void Reset()
    {
        _bpms = new List<double>();
        _lastTime = DateTime.MinValue;
        TapBtn.Content = "Tap";
    }
}