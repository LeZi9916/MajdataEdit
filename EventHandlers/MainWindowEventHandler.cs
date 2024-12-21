using Avalonia.Controls;
using Avalonia.Interactivity;
using MajdataEdit.Utils;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit;
public partial class MainWindow : Window
{
    BPMTap? _bpmTapForm = null;
    async void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        var result = await CheckUpdate();
        if (!result.IsSuccess)
            return;
        else if (result.LatestVersion is null || result.LatestVersion.ToVersion() <= MajEnvironment.Version)
            return;
        var url = result.DownloadUrl ?? "https://github.com/LingFeng-bbben/MajdataView/releases";
        await UpdateNotify(url);
    }
    async void OnMenuCheckUpdateClick(object? sender, RoutedEventArgs e)
    {
        var result = await CheckUpdate();

        if (!result.IsSuccess || result.IsBusy)
            return;
        else if (result.LatestVersion is null)
            return;
        else if (result.LatestVersion.ToVersion() <= MajEnvironment.Version)
        {
            await MessageBox.ShowAsync("Check update", "No updates available", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
            return;
        }
        var url = result.DownloadUrl ?? "https://github.com/LingFeng-bbben/MajdataView/releases";
        await UpdateNotify(url);
    }
    void OnLevelSelctorChanged(object? sender, RoutedEventArgs e)
    {

    }
    private void OnMenuMajnetClick(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo() { FileName = "https://majdata.net", UseShellExecute = true });
    }
    private void OnMenuGithubClick(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo() { FileName = "https://github.com/LingFeng-bbben/MajdataView", UseShellExecute = true });
    }
    void OnMenuBPMTapClick(object? sender, RoutedEventArgs e)
    {
        if (_bpmTapForm is not null)
            return;
        _bpmTapForm = new BPMTap();
        _bpmTapForm.Closed += (_, _) => _bpmTapForm = null;
        _bpmTapForm.Show(this);
    }
}
