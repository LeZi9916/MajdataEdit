using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit;
public partial class BPMTap : Window
{
    void OnTapBtnClick(object? sender, RoutedEventArgs e)
    {
        Update();
    }
    void OnResetBtnClick(object? sender, RoutedEventArgs e)
    {
        Reset();
    }
}
