using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using DiscordRPC.Logging;
using Microsoft.Win32;
using Un4seen.Bass;
using Timer = System.Timers.Timer;
using MajdataEdit.Modules.SyntaxModule;
using MajdataEdit.Modules.AutoSaveModule;
using MajdataEdit.Types;
using MajdataEdit.Utils;

namespace MajdataEdit;

/// <summary>
///     MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        if (Environment.GetCommandLineArgs().Contains("--ForceSoftwareRender"))
        {
            MessageBox.Show("正在以软件渲染模式运行\nソフトウェア・レンダリング・モードで動作\nBooting as software rendering mode.");
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        CheckAndStartView();

        TheWindow.Title = GetWindowsTitleString();

        SetWindowGoldenPosition();

        DCRPCclient.Logger = new ConsoleLogger { Level = LogLevel.Warning };
        DCRPCclient.Initialize();

        
        InitWave();

        AudioManager.Init(new WindowInteropHelper(this).Handle);
        await ReadEditorSettingAsync();
        ChartRefreshDelay = editorSetting.ChartRefreshDelay;
        VisualEffectUpdater(16);
        ChartUpdater();

        waveStopMonitorTimer.Elapsed += WaveStopMonitorTimer_Elapsed;
        playbackSpeedHideTimer.Elapsed += PlbHideTimer_Elapsed;

        if (editorSetting!.AutoCheckUpdate) 
        {
            CheckUpdateButton.IsEnabled = false;
            await CheckUpdate(true);
            CheckUpdateButton.IsEnabled = true;
        }
        ViewMonitor.Init();
        #region 异常退出处理

        if (!SafeTerminationDetector.Of().IsLastTerminationSafe())
        {
            // 若上次异常退出，则询问打开恢复窗口
            var result = MessageBox.Show(GetLocalizedString("AbnormalTerminationInformation"),
                GetLocalizedString("Attention"), MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                var lastEditPath = File.ReadAllText(SafeTerminationDetector.Of().RecordPath).Trim();
                if (lastEditPath.Length != 0)
                    // 尝试打开上次未正常关闭的谱面 然后再打开恢复页面
                    try
                    {
                        await initFromFile(lastEditPath);
                    }
                    catch (Exception error)
                    {
                        Console.WriteLine(error.StackTrace);
                    }

                Menu_AutosaveRecover_Click(new object(), new RoutedEventArgs());
            }
        }

        SafeTerminationDetector.Of().RecordProgramClose();

        #endregion
    }


    //start the view and wait for boot, then set window pos
    private void SetWindowPosTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        var setWindowPosTimer = (Timer)sender!;
        Dispatcher.Invoke(() => { InternalSwitchWindow(); });
        setWindowPosTimer.Stop();
        setWindowPosTimer.Dispose();
    }

    //Window events
    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!isSaved)
            if (!AskSave())
            {
                e.Cancel = true;
                return;
            }

        var process = Process.GetProcessesByName("MajdataView");
        if (process.Length > 0)
        {
            var result = MessageBox.Show(GetLocalizedString("AskCloseView"), GetLocalizedString("Attention"),
                MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
                process[0].Kill();
        }

        soundSetting.Close();
        //if (bpmtap != null) { bpmtap.Close(); }
        //if (muriCheck != null) { muriCheck.Close(); }
        SaveSettingAsync().AsTask().Wait();

        AudioManager.Disposal();

        // 正常退出
        SafeTerminationDetector.Of().RecordProgramClose();
    }

    //Window grid events
    private void Grid_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.Move;
    }

    private async void Grid_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            //Console.WriteLine(e.Data.GetData(DataFormats.FileDrop).ToString());
            if (e.Data.GetData(DataFormats.FileDrop).ToString() == "System.String[]")
            {
                var path = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                if (path.ToLower().Contains("maidata.txt"))
                {
                    if (!isSaved)
                        if (!AskSave())
                            return;
                    var fileInfo = new FileInfo(path);
                    await initFromFile(fileInfo.DirectoryName!);
                }
            }
    }

    private void FindClose_MouseDown(object sender, MouseButtonEventArgs e)
    {
        FindGrid.Visibility = Visibility.Collapsed;
        FumenContent.Focus();
    }


    
}