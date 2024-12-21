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
    async void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        var result = await CheckUpdate();
        if (!result.IsSuccess)
            return;
        else if (result.LatestVersion is null || result.LatestVersion.ToVersion() <= MajEnvironment.Version)
            return;
        var url = result.DownloadUrl ?? "https://github.com/LingFeng-bbben/MajdataView/releases";
        UpdateNotify(url);
    }
    async void OnMenuCheckUpdateClick(object? sender, RoutedEventArgs e)
    {
        var result = await CheckUpdate();

        if (!result.IsSuccess)
            return;
        else if (result.LatestVersion is null || result.LatestVersion.ToVersion() <= MajEnvironment.Version)
            return;
    }
    async Task UpdateNotify(string url)
    {
        var boxParams = new MessageBoxCustomParams
        {
            ButtonDefinitions = new List<ButtonDefinition>
                {
                    new ButtonDefinition { Name = "Yes", },
                    new ButtonDefinition { Name = "No", },
                },
            ContentTitle = "Update available",
            ContentMessage = "New version had resleased",
            Icon = MsBox.Avalonia.Enums.Icon.Question,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            MaxWidth = 500,
            MaxHeight = 800,
            SizeToContent = SizeToContent.WidthAndHeight,
            ShowInCenter = true,
            Topmost = true,
        };
        var box = MessageBoxManager.GetMessageBoxCustom(boxParams);
        switch (await box.ShowAsync())
        {
            case "Yes":
                Platform.OpenUrl(url);
                break;
            default:
                return;
        }
    }
    private void OnMenuMajnetClick(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo() { FileName = "https://majdata.net", UseShellExecute = true });
    }
    private void OnMenuGithubClick(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo() { FileName = "https://github.com/LingFeng-bbben/MajdataView", UseShellExecute = true });
    }
}
