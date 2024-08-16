using MajdataEdit.Modules.SyntaxModule;
using MajdataEdit.Types;
using MajdataEdit.Utils;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Timer = System.Timers.Timer;

namespace MajdataEdit;
public partial class MainWindow : Window
{
    async ValueTask Pause()
    {
        var rsp = await ViewController.Pause();
        switch(rsp)
        {
            case RequestState.TimeOut:
                MessageBox.Show(GetLocalizedString("AskRender"), GetLocalizedString("Attention"));
                return;
            case RequestState.Invaild:
                MessageBox.Show("Unknown Error","Error",MessageBoxButton.OK,MessageBoxImage.Error);
                return;
        }
        FumenContent.Focus();
        PlayAndPauseButton.Content = "▶";

        AudioManager.Pause(ChannelType.BGM);
        AudioManager.Pause(ChannelType.HoldRiser);

        waveStopMonitorTimer.Stop();
    }
    async ValueTask Play()
    {
        var CusorTime = SimaiProcessor.Serialize(GetRawFumenText(), GetRawFumenPosition()); //scan first
        var isOpIncluded = false;

        FumenContent.Focus();
        await SaveFumen();
        
        lastPlayTiming = AudioManager.GetSeconds(ChannelType.BGM);
        await GenerateSoundEffectList(lastPlayTiming, isOpIncluded);
        SimaiProcessor.ClearNoteListPlayedState();

        var rsp = await ViewController.Play();
        switch (rsp)
        {
            case RequestState.TimeOut:
                MessageBox.Show(GetLocalizedString("AskRender"), GetLocalizedString("Attention"));
                return;
            case RequestState.Invaild:
                MessageBox.Show("Unknown Error", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
        }
        PlayAndPauseButton.Content = "  ▌▌ ";
        StartSELoop();
        waveStopMonitorTimer.Start();
        AudioManager.Play(ChannelType.BGM, false);
        ghostCusorPositionTime = (float)CusorTime;
    }
    async ValueTask Preview()
    {
        var startAt = DateTime.Now.AddSeconds(5d);
        await GenerateSoundEffectList(0.0, true);
        InternalSwitchWindow(false);
        
        AudioManager.SetPosition(ChannelType.BGM, 0);
        
        var rsp = await ViewController.Preview();
        switch (rsp)
        {
            case RequestState.TimeOut:
                MessageBox.Show(GetLocalizedString("AskRender"), GetLocalizedString("Attention"));
                return;
            case RequestState.Invaild:
                MessageBox.Show("Unknown Error", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
        }
        lastPlayTiming = 0;
        AudioManager.Play(ChannelType.TrackStart, true);
        await Task.Delay(startAt - DateTime.Now);
        SimaiProcessor.ClearNoteListPlayedState();
        StartSELoop();
        waveStopMonitorTimer.Start();
        AudioManager.Play(ChannelType.BGM, false);
        ghostCusorPositionTime = 0;
    }
    async ValueTask Stop()
    {
        await Dispatcher.InvokeAsync(FumenContent.Focus);
        var rsp = await ViewController.Stop();
        switch (rsp)
        {
            case RequestState.TimeOut:
                MessageBox.Show(GetLocalizedString("AskRender"), GetLocalizedString("Attention"));
                return;
            case RequestState.Invaild:
                MessageBox.Show("Unknown Error", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
        }

        PlayAndPauseButton.Content = "▶";
        AudioManager.Stop(ChannelType.BGM);
        AudioManager.Stop(ChannelType.HoldRiser);

        waveStopMonitorTimer.Stop();
        AudioManager.SetSeconds(ChannelType.BGM, lastPlayTiming);
    }
    async ValueTask Continue()
    {
        var CusorTime = SimaiProcessor.Serialize(GetRawFumenText(), GetRawFumenPosition()); //scan first
        lastPlayTiming = AudioManager.GetSeconds(ChannelType.BGM);

        var rsp = await ViewController.Continue();
        switch (rsp)
        {
            case RequestState.TimeOut:
                MessageBox.Show(GetLocalizedString("AskRender"), GetLocalizedString("Attention"));
                return;
            case RequestState.Invaild:
                MessageBox.Show("Unknown Error", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
        }
        PlayAndPauseButton.Content = "  ▌▌ ";
        waveStopMonitorTimer.Start();
        AudioManager.Play(ChannelType.BGM, false);
        ghostCusorPositionTime = (float)CusorTime;
    }


    private async ValueTask TogglePause()
    {
        isPlaying = false;
        isPlan2Stop = false;

        FumenContent.Focus();
        PlayAndPauseButton.Content = "▶";

        AudioManager.Pause(ChannelType.BGM);
        AudioManager.Pause(ChannelType.HoldRiser);

        waveStopMonitorTimer.Stop();
        await RequestToPause();
    }
    private async ValueTask TogglePlay(PlayMethod playMethod = PlayMethod.Normal)
    {
        if (EditorState == EditorControlMethod.Start || playMethod != PlayMethod.Normal)
            if (!await RequestToStop())
                return;

        FumenContent.Focus();
        await SaveFumen();
        if (CheckAndStartView()) 
            return;
        isPlaying = true;
        isPlan2Stop = false;
        PlayAndPauseButton.Content = "  ▌▌ ";
        var CusorTime = SimaiProcessor.Serialize(GetRawFumenText(), GetRawFumenPosition()); //scan first

        //TODO: Moeying改一下你的generateSoundEffect然后把下面这行删了
        var isOpIncluded = playMethod == PlayMethod.Normal ? false : true;

        var startAt = DateTime.Now;
        switch (playMethod)
        {
            case PlayMethod.Record:
                AudioManager.SetPosition(ChannelType.BGM,0);

                startAt = DateTime.Now.AddSeconds(5d);
                //TODO: i18n
                MessageBox.Show(GetLocalizedString("AskRender"), GetLocalizedString("Attention"));
                InternalSwitchWindow(false);

                await GenerateSoundEffectList(0.0, isOpIncluded);
                await Task.Run(() => RenderSoundEffect(5d));

                if (!await RequestToRun(startAt, playMethod)) return;
                break;
            case PlayMethod.Op:
                await GenerateSoundEffectList(0.0, isOpIncluded);
                InternalSwitchWindow(false);
                AudioManager.SetPosition(ChannelType.BGM, 0);
                startAt = DateTime.Now.AddSeconds(5d);
                AudioManager.Play(ChannelType.TrackStart, true);

                if (!await RequestToRun(startAt, playMethod)) 
                    return;

                await Task.Delay(startAt - DateTime.Now);
                Dispatcher.Invoke(() =>
                {
                    lastPlayTiming = AudioManager.GetSeconds(ChannelType.BGM);
                    SimaiProcessor.ClearNoteListPlayedState();
                    StartSELoop();

                    waveStopMonitorTimer.Start();
                    AudioManager.Play(ChannelType.BGM, false);
                });
                break;
            case PlayMethod.Normal:
                lastPlayTiming = AudioManager.GetSeconds(ChannelType.BGM);
                await GenerateSoundEffectList(lastPlayTiming, isOpIncluded);
                SimaiProcessor.ClearNoteListPlayedState();
                StartSELoop();
                waveStopMonitorTimer.Start();
                startAt = DateTime.Now;
                AudioManager.Play(ChannelType.BGM, false);
                if (EditorState == EditorControlMethod.Pause)
                {
                    if (!await RequestToContinue(startAt)) return;
                }
                else
                {
                    if (!await RequestToRun(startAt, playMethod)) return;
                }
                break;
        }

        ghostCusorPositionTime = (float)CusorTime;
    }
    private async ValueTask ToggleStop()
    {
        isPlaying = false;
        isPlan2Stop = false;

        await Dispatcher.InvokeAsync(FumenContent.Focus);
        PlayAndPauseButton.Content = "▶";
        AudioManager.Stop(ChannelType.BGM);
        AudioManager.Stop(ChannelType.HoldRiser);

        waveStopMonitorTimer.Stop();
        await RequestToStop();
        AudioManager.SetSeconds(ChannelType.BGM, lastPlayTiming);
    }
    private async ValueTask TogglePlayAndPause(PlayMethod playMethod = PlayMethod.Normal)
    {
        SetControlButtonActive(false);
        if (ViewController.IsPlaying)
        {
            //await TogglePause();
            await Pause();
            SetControlButtonActive(true);
        }
        else
        {
            //if (EditorState != EditorControlMethod.Pause &&
            //    EditorSetting!.SyntaxCheckLevel == 2 &&
            //    SyntaxChecker.GetErrorCount() != 0)
            //{
            //    ShowErrorWindow();
            //    return;
            //}
            //await TogglePlay(playMethod);
            await Play();
            SetControlButtonActive(true);
            Op_Button.IsEnabled = false;
        }

    }
    private async ValueTask TogglePlayAndStop(PlayMethod playMethod = PlayMethod.Normal)
    {
        SetControlButtonActive(false);
        if (EditorSetting!.SyntaxCheckLevel == 2 && SyntaxChecker.GetErrorCount() != 0)
        {
            ShowErrorWindow();
            return;
        }
        if (isPlaying)
        {
            await ToggleStop();
            SetControlButtonActive(true);
        }
        else
        {
            await TogglePlay(playMethod);
            SetControlButtonActive(true);
            Op_Button.IsEnabled = false;
        }
    }



    //*VIEW COMMUNICATION
    /// <summary>
    /// 向View发送Stop请求
    /// </summary>
    /// <returns></returns>
    private async ValueTask<bool> RequestToStop()
    {
        var req = new EditRequest
        {
            Control = EditorControlMethod.Stop
        };
        var response = await WebControl.RequestPostAsync("http://localhost:8013/", req);
        if (!response.IsSuccess)
        {
            MessageBox.Show(GetLocalizedString("PortClear"));
            return false;
        }

        EditorState = EditorControlMethod.Stop;
        return true;
    }
    /// <summary>
    /// 向View发送Pause请求
    /// </summary>
    /// <returns></returns>
    private async ValueTask<bool> RequestToPause()
    {
        var req = new EditRequest
        {
            Control = EditorControlMethod.Pause
        };
        var response = await WebControl.RequestPostAsync("http://localhost:8013/", req);
        if (!response.IsSuccess)
        {
            MessageBox.Show(GetLocalizedString("PortClear"));
            return false;
        }

        EditorState = EditorControlMethod.Pause;
        return true;
    }
    /// <summary>
    /// 向View发送Continue请求
    /// </summary>
    /// <param name="StartAt"></param>
    /// <returns></returns>
    private async ValueTask<bool> RequestToContinue(DateTime StartAt)
    {
        var req = new EditRequest
        {
            Control = EditorControlMethod.Continue,
            StartAt = StartAt.Ticks,
            StartTime = (float)AudioManager.GetSeconds(ChannelType.BGM),
            AudioSpeed = PlaybackSpeed,
            EditorPlayMethod = EditorSetting!.editorPlayMethod
        };
        var response = await WebControl.RequestPostAsync("http://localhost:8013/", req);
        if (!response.IsSuccess)
        {
            MessageBox.Show(GetLocalizedString("PortClear"));
            return false;
        }

        EditorState = EditorControlMethod.Start;
        return true;
    }
    /// <summary>
    /// 向View发送Play请求
    /// </summary>
    /// <param name="StartAt"></param>
    /// <param name="playMethod"></param>
    /// <returns></returns>
    private async ValueTask<bool> RequestToRun(DateTime StartAt, PlayMethod playMethod)
    {
        var path = Path.Combine(MaidataDir, "majdata.json");

        await MajsonGenerator.Generate(path, SelectedDifficulty);

        EditorControlMethod control = playMethod switch
        {
            PlayMethod.Op => EditorControlMethod.OpStart,
            PlayMethod.Normal => EditorControlMethod.Start,
            _ => EditorControlMethod.Record
        };

        // 将maimaiDX速度换算为View中的单位速度 MajSpeed = 107.25 / (71.4184491 * (MaiSpeed + 0.9975) ^ -0.985558604)

        var req = new EditRequest()
        {
            Control = control,
            JsonPath = path,
            StartAt = StartAt.Ticks,
            StartTime = (float)AudioManager.GetSeconds(ChannelType.BGM),
            NoteSpeed = EditorSetting!.NoteSpeed,
            TouchSpeed = EditorSetting!.TouchSpeed,
            BackgroundCover = EditorSetting!.backgroundCover,
            ComboStatusType = EditorSetting!.comboStatusType,
            AudioSpeed = PlaybackSpeed,
            SmoothSlideAnime = EditorSetting!.SmoothSlideAnime,
            EditorPlayMethod = EditorSetting.editorPlayMethod
        };

        var response = await WebControl.RequestPostAsync("http://localhost:8013/", req);
        if (!response.IsSuccess)
        {
            MessageBox.Show(GetLocalizedString("PortClear"));
            return false;
        }

        EditorState = EditorControlMethod.Start;
        return true;
    }
    /// <summary>
    /// 拉起MajdataView
    /// </summary>
    /// <returns></returns>
    private bool CheckAndStartView()
    {
        var path = "MajdataView.exe";
        if (Process.GetProcessesByName("MajdataView").Length == 0 && Process.GetProcessesByName("Unity").Length == 0)
        {
            if (!File.Exists(path))
                return true;
            var viewProcess = Process.Start(path);
            var setWindowPosTimer = new Timer(2000)
            {
                AutoReset = false
            };
            setWindowPosTimer.Elapsed += SetWindowPosTimer_Elapsed;
            setWindowPosTimer.Start();
            return true;
        }

        return false;
    }
    /// <summary>
    /// 获取MajdataView的工作目录
    /// </summary>
    /// <returns></returns>
    private string GetViewerWorkingDirectory() => Environment.CurrentDirectory + "/MajdataView_Data/StreamingAssets";
}
public static class ViewController
{
    public static double LastPlayTiming { get; set; } = 0;
    public static EditorState State { get; private set; } = EditorState.Idle;
    public static bool IsPlaying => State == EditorState.Playing;
    public static bool IsPaused => State == EditorState.Paused;
    public static bool IsIdle => State == EditorState.Idle;

    static CancellationTokenSource? lastTaskSouce = null;

    public static async ValueTask<RequestState> Pause()
    {
        if(!IsPlaying)
            return RequestState.Invaild;

        AudioManager.Pause(ChannelType.BGM);
        AudioManager.Pause(ChannelType.HoldRiser);

        if (await RequestToPause())
        {
            State = EditorState.Paused;
            return RequestState.OK;
        }
        else
            return RequestState.TimeOut;
    }
    public static async ValueTask<RequestState> Continue()
    {
        if (IsPlaying || !IsPaused)
            return RequestState.Invaild;

        LastPlayTiming = AudioManager.GetSeconds(ChannelType.BGM);
        var result = await RequestToContinue();
        AudioManager.Play(ChannelType.BGM, false);

        if (result)
        {
            State = EditorState.Playing;
            return RequestState.OK;
        }
        else
            return RequestState.TimeOut;
    }
    public static async ValueTask<RequestState> Play()
    {
        if (IsPlaying)
            return RequestState.Invaild;
        else if (IsPaused)
            return await Continue();

        if (await RequestToRun(PlayMethod.Normal))
        {
            if(lastTaskSouce is not null && !lastTaskSouce.IsCancellationRequested)
                lastTaskSouce.Cancel();
            lastTaskSouce = new();
            State = EditorState.Playing;
            FinishedDetector(1000,lastTaskSouce.Token);
            return RequestState.OK;
        }
        else
            return RequestState.Invaild;
    }
    public static async ValueTask<RequestState> Preview()
    {
        if (IsPlaying)
            return RequestState.Invaild;
        LastPlayTiming = 0;
        if (await RequestToRun(PlayMethod.Op, DateTime.Now.AddSeconds(5d)))
        {
            State = EditorState.Playing;
            return RequestState.OK;
        }
        else
            return RequestState.Invaild;
    }
    public static async ValueTask<RequestState> Record()
    {
        if (IsPlaying)
            return RequestState.Invaild;

        LastPlayTiming = 0;
        if (await RequestToRun(PlayMethod.Record, DateTime.Now.AddSeconds(5d)))
        {
            State = EditorState.Idle;
            return RequestState.OK;
        }
        else
            return RequestState.Invaild;
    }
    public static async ValueTask<RequestState> Stop()
    {
        if (!IsPlaying && !IsPaused)
            return RequestState.Invaild;

        if(await RequestToStop())
        {
            if (lastTaskSouce is not null)
                lastTaskSouce.Cancel();
            State = EditorState.Idle;
            return RequestState.OK;
        }
        else
            return RequestState.TimeOut;

    }

    static async void FinishedDetector(int extraTime,CancellationToken token)
    {
        await Task.Delay(200);
        while(true)
        {
            if (token.IsCancellationRequested)
                break;
            if (IsPaused)
            {
                await Task.Delay(100);
                continue;
            }
            else if (IsPlaying)
            {
                if (!AudioManager.ChannelIsPlaying(ChannelType.BGM))
                {
                    await Task.Delay(extraTime);
                    await Stop();
                    break;
                }
            }
            else
                break;
            await Task.Delay(100);
        }
    }

    //*VIEW COMMUNICATION
    /// <summary>
    /// 向View发送Stop请求
    /// </summary>
    /// <returns></returns>
    private static async ValueTask<bool> RequestToStop()
    {
        var req = new EditRequest
        {
            Control = EditorControlMethod.Stop
        };

        return (await WebControl.RequestPostAsync("http://localhost:8013/", req)).IsSuccess;
    }
    /// <summary>
    /// 向View发送Pause请求
    /// </summary>
    /// <returns></returns>
    private static async ValueTask<bool> RequestToPause()
    {
        var req = new EditRequest
        {
            Control = EditorControlMethod.Pause
        };

        return (await WebControl.RequestPostAsync("http://localhost:8013/", req)).IsSuccess;
    }
    /// <summary>
    /// 向View发送Continue请求
    /// </summary>
    /// <param name="StartAt"></param>
    /// <returns></returns>
    private static async ValueTask<bool> RequestToContinue()
    {
        var setting = MainWindow.EditorSetting;

        var req = new EditRequest
        {
            Control = EditorControlMethod.Continue,
            StartAt = DateTime.Now.Ticks,
            StartTime = (float)AudioManager.GetSeconds(ChannelType.BGM),
            AudioSpeed = MainWindow.PlaybackSpeed,
            EditorPlayMethod = setting!.editorPlayMethod
        };
        return (await WebControl.RequestPostAsync("http://localhost:8013/", req)).IsSuccess;
    }
    /// <summary>
    /// 向View发送Play请求
    /// </summary>
    /// <param name="StartAt"></param>
    /// <param name="playMethod"></param>
    /// <returns></returns>
    private static async ValueTask<bool> RequestToRun(PlayMethod playMethod,DateTime? startAt = null)
    {
        if (startAt is null)
            startAt = DateTime.Now;

        var path = Path.Combine(MainWindow.MaidataDir, "majdata.json");
        var setting = MainWindow.EditorSetting;

        await MajsonGenerator.Generate(path, MainWindow.SelectedDifficulty);

        EditorControlMethod control = playMethod switch
        {
            PlayMethod.Op => EditorControlMethod.OpStart,
            PlayMethod.Normal => EditorControlMethod.Start,
            _ => EditorControlMethod.Record
        };

        // 将maimaiDX速度换算为View中的单位速度 MajSpeed = 107.25 / (71.4184491 * (MaiSpeed + 0.9975) ^ -0.985558604)

        var req = new EditRequest()
        {
            Control = control,
            JsonPath = path,
            StartAt = ((DateTime)startAt).Ticks,
            StartTime = (float)AudioManager.GetSeconds(ChannelType.BGM),
            NoteSpeed = setting!.NoteSpeed,
            TouchSpeed = setting!.TouchSpeed,
            BackgroundCover = setting!.backgroundCover,
            ComboStatusType = setting!.comboStatusType,
            AudioSpeed = MainWindow.PlaybackSpeed,
            SmoothSlideAnime = setting!.SmoothSlideAnime,
            EditorPlayMethod = setting.editorPlayMethod
        };

        return (await WebControl.RequestPostAsync("http://localhost:8013/", req)).IsSuccess;
    }
}
