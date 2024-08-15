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
    private async ValueTask TogglePause()
    {
        Op_Button.IsEnabled = true;
        isPlaying = false;
        isPlan2Stop = false;

        FumenContent.Focus();
        PlayAndPauseButton.Content = "▶";

        AudioManager.Pause(ChannelType.BGM);
        AudioManager.Pause(ChannelType.HoldRiser);

        waveStopMonitorTimer.Stop();
        visualEffectRefreshTimer.Stop();
        await RequestToPause();
        await DrawWave();
    }
    private async ValueTask TogglePlay(PlayMethod playMethod = PlayMethod.Normal)
    {
        //if (!Op_Button.IsEnabled) return;

        if (EditorState == EditorControlMethod.Start || playMethod != PlayMethod.Normal)
            if (!await RequestToStop())
                return;

        FumenContent.Focus();
        await SaveFumen();
        if (CheckAndStartView()) return;
        Op_Button.IsEnabled = false;
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
                    playStartTime = AudioManager.GetSeconds(ChannelType.BGM);
                    SimaiProcessor.ClearNoteListPlayedState();
                    StartSELoop();

                    waveStopMonitorTimer.Start();
                    visualEffectRefreshTimer.Start();
                    AudioManager.Play(ChannelType.BGM, false);
                });
                break;
            case PlayMethod.Normal:
                playStartTime = AudioManager.GetSeconds(ChannelType.BGM);
                await GenerateSoundEffectList(playStartTime, isOpIncluded);
                SimaiProcessor.ClearNoteListPlayedState();
                StartSELoop();
                //soundEffectTimer.Start();
                waveStopMonitorTimer.Start();
                visualEffectRefreshTimer.Start();
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
        await DrawWave();
    }
    private async ValueTask ToggleStop()
    {
        Op_Button.IsEnabled = true;
        isPlaying = false;
        isPlan2Stop = false;

        FumenContent.Focus();
        PlayAndPauseButton.Content = "▶";
        AudioManager.Stop(ChannelType.BGM);
        AudioManager.Stop(ChannelType.HoldRiser);

        waveStopMonitorTimer.Stop();
        visualEffectRefreshTimer.Stop();
        await RequestToStop();
        AudioManager.SetSeconds(ChannelType.BGM, playStartTime);
        await DrawWave();
    }
    private async ValueTask TogglePlayAndPause(PlayMethod playMethod = PlayMethod.Normal)
    {
        if (isPlaying)
            await TogglePause();
        else
        {
            if (EditorState != EditorControlMethod.Pause &&
                editorSetting!.SyntaxCheckLevel == 2 &&
                SyntaxChecker.GetErrorCount() != 0)
            {
                ShowErrorWindow();
                return;
            }
            await TogglePlay(playMethod);
        }

    }
    private async ValueTask TogglePlayAndStop(PlayMethod playMethod = PlayMethod.Normal)
    {
        if (editorSetting!.SyntaxCheckLevel == 2 && SyntaxChecker.GetErrorCount() != 0)
        {
            ShowErrorWindow();
            return;
        }
        if (isPlaying)
            await ToggleStop();
        else
            await TogglePlay(playMethod);
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
            AudioSpeed = GetPlaybackSpeed(),
            EditorPlayMethod = editorSetting!.editorPlayMethod
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
        float startTime = 0;

        await MajsonGenerator.Generate(path, selectedDifficulty);

        EditorControlMethod control = playMethod switch
        {
            PlayMethod.Op => EditorControlMethod.OpStart,
            PlayMethod.Normal => EditorControlMethod.Start,
            _ => EditorControlMethod.Record
        };

        Dispatcher.Invoke(() =>
        {
            startTime = (float)AudioManager.GetSeconds(ChannelType.BGM);
            // request.playSpeed = float.Parse(ViewerSpeed.Text);
            // 将maimaiDX速度换算为View中的单位速度 MajSpeed = 107.25 / (71.4184491 * (MaiSpeed + 0.9975) ^ -0.985558604)
        });

        var req = new EditRequest()
        {
            Control = control,
            JsonPath = path,
            StartAt = StartAt.Ticks,
            StartTime = startTime,
            NoteSpeed = editorSetting!.NoteSpeed,
            TouchSpeed = editorSetting!.TouchSpeed,
            BackgroundCover = editorSetting!.backgroundCover,
            ComboStatusType = editorSetting!.comboStatusType,
            AudioSpeed = GetPlaybackSpeed(),
            SmoothSlideAnime = editorSetting!.SmoothSlideAnime,
            EditorPlayMethod = editorSetting.editorPlayMethod
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
