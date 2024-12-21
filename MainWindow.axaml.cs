using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MajdataEdit.Types;
using MajdataEdit.Utils;
using MajSimaiDecode;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;
using Semver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MajdataEdit;
public partial class MainWindow : Window
{
    public SimaiProcess Chart { get; private set; } = new SimaiProcess("");
    readonly Lock _checkUpdateLock = new();
    readonly HttpClient _httpClient = new();
    public MainWindow()
    {
        InitializeComponent();
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
        Cover.IsVisible = true;
    }
    async ValueTask<CheckUpdateResult> CheckUpdate()
    {
        if (!_checkUpdateLock.TryEnter())
        {
            return new()
            {
                IsSuccess = false,
                IsBusy = true,
            };
        }
        MenuCheckUpdate.Header = "Checking update...";
        MenuCheckUpdate.IsEnabled = false;
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "http://api.github.com/repos/LingFeng-bbben/MajdataView/releases/latest");
            req.Headers.Add("User-Agent", $"MajdataEdit / {MajEnvironment.Version}");
            var rsp = (await _httpClient.SendAsync(req)).EnsureSuccessStatusCode();
            var rspObj = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(await rsp.Content.ReadAsStreamAsync());
            var latestVersionString = rspObj?.TryGetValue("tag_name", out var s) ?? false ? s.ToString() : string.Empty;
            var latestVersion = SemVersion.TryParse(latestVersionString, SemVersionStyles.Any, out var v) ? v : null;
            var releaseUrl = rspObj?.TryGetValue("html_url", out var ss) ?? false ? ss.ToString() : string.Empty;
            var isInvalidRsp = string.IsNullOrEmpty(latestVersionString) || string.IsNullOrEmpty(releaseUrl) || latestVersion is null;
            if (isInvalidRsp)
            {
                return new()
                {
                    IsSuccess = false,
                    IsBusy = false,
                    StatusCode = rsp.StatusCode,
                    LatestVersion = null,
                    DownloadUrl = string.Empty,
                    HttpRequestError = null
                };
            }
            return new()
            {
                IsSuccess = true,
                IsBusy = false,
                StatusCode = rsp.StatusCode,
                LatestVersion = latestVersion,
                DownloadUrl = releaseUrl,
                HttpRequestError = null
            };
        }
        catch(HttpRequestException e)
        {
            return new()
            {
                IsSuccess = false,
                IsBusy = false,
                StatusCode = e.StatusCode,
                LatestVersion = null,
                DownloadUrl = null,
                HttpRequestError = e.HttpRequestError,
                Exception = e
            };
        }
        catch(Exception e)
        {
            return new()
            {
                IsSuccess = false,
                IsBusy = false,
                StatusCode = null,
                LatestVersion = null,
                DownloadUrl = null,
                HttpRequestError = null,
                Exception = e
            };
        }
        finally
        {
            _checkUpdateLock.Exit();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MenuCheckUpdate.Header = "CheckUpdate";
                MenuCheckUpdate.IsEnabled = true;
            });
        }
    }
    async Task UpdateNotify(string url)
    {
        var boxParams = new MessageBoxCustomParams
        {
            ButtonDefinitions =
            [
                new ButtonDefinition { Name = "Yes", },
                new ButtonDefinition { Name = "No", },
            ],
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
        switch (await MessageBox.ShowAsync(boxParams))
        {
            case "Yes":
                Platform.OpenUrl(url);
                break;
            default:
                return;
        }
    }
    async Task ReadChartFromFile<T>(T file) where T: IStorageFile
    {
        using var fileStream = await file.OpenReadAsync();
        using var memoryBuffer = new MemoryStream();
        await fileStream.CopyToAsync(memoryBuffer);
        var chartStr = Encoding.UTF8.GetString(memoryBuffer.ToArray());
        var chart = new SimaiProcess(chartStr);
        
    }
}