using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MajdataEdit.Types;
using Semver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MajdataEdit;
public partial class MainWindow : Window
{

    readonly Lock _checkUpdateLock = new();
    readonly HttpClient _httpClient = new();
    public MainWindow()
    {
        InitializeComponent();
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
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
}