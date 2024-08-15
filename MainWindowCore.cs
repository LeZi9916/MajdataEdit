﻿using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using DiscordRPC;
using MajdataEdit.Modules.AutoSaveModule;
using MajdataEdit.Modules.SyntaxModule;
using MajdataEdit.Types;
using MajdataEdit.Utils;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Semver;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;
using WPFLocalizeExtension.Engine;
using WPFLocalizeExtension.Extensions;
using Brush = System.Drawing.Brush;
using Color = System.Drawing.Color;
using DashStyle = System.Drawing.Drawing2D.DashStyle;
using JsonSerializer = System.Text.Json.JsonSerializer;
using LinearGradientBrush = System.Drawing.Drawing2D.LinearGradientBrush;
using Pen = System.Drawing.Pen;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Timer = System.Timers.Timer;

namespace MajdataEdit;

public partial class MainWindow : Window
{
    const string majSettingFilename = "majSetting.json";
    const string editorSettingFilename = "EditorSetting.json";
    public static readonly string MAJDATA_VERSION_STRING = $"v{Assembly.GetExecutingAssembly().GetName().Version!.ToString(3)}";
    public static readonly SemVersion MAJDATA_VERSION = SemVersion.Parse(MAJDATA_VERSION_STRING, SemVersionStyles.Any);

    public static string MaidataDir { get; private set; } = "";

    //float[] wavedBs;
    readonly short[][] waveRaws = new short[3][];
    public Timer chartChangeTimer = new(1000); // 谱面变更延迟解析]\
    readonly Timer currentTimeRefreshTimer = new(100);

    DiscordRpcClient DCRPCclient = new("1068882546932326481");

    float deltatime = 4f;
    public EditorSetting? editorSetting;

    bool fumenOverwriteMode; //谱面文本覆盖模式
    float ghostCusorPositionTime;
    bool isDrawing;
    bool isLoading;
    bool isReplaceConformed;

    bool isSaved = true;
    public EditorControlMethod EditorState { get; private set; }
    TextSelection? lastFindPosition;

    double lastMousePointX; //Used for drag scroll

    int selectedDifficulty = -1;
    double songLength;

    SoundSetting soundSetting = new();
    bool UpdateCheckLock;


    //*UI DRAWING
    readonly Timer visualEffectRefreshTimer = new(16.6667);

    WriteableBitmap? WaveBitmap;

    //*TEXTBOX CONTROL
    private string GetRawFumenText()
    {
        var text = new TextRange(FumenContent.Document.ContentStart, FumenContent.Document.ContentEnd).Text!;

        text = text.Replace("\r", "");
        // 亲爱的bbben在这里对text进行了Trim 引发了行位置不正确的BUG 谨此纪念（
        return text;
    }

    private void SetRawFumenText(string content)
    {
        isLoading = true;
        FumenContent.Document.Blocks.Clear();
        if (content == null)
        {
            isLoading = false;
            return;
        }

        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(line);
            FumenContent.Document.Blocks.Add(paragraph);
        }

        isLoading = false;
    }

    private long GetRawFumenPosition()
    {
        long pos = new TextRange(FumenContent.Document.ContentStart, FumenContent.CaretPosition).Text.Replace("\r", "")
            .Length;
        return pos;
    }

    private void SeekTextFromTime()
    {
        //Console.WriteLine("SeekText");
        var time = AudioManager.GetSeconds(ChannelType.BGM);
        var timingList = new List<SimaiTimingPoint>();
        timingList.AddRange(SimaiProcessor.timinglist);
        var noteList = SimaiProcessor.notelist;
        if (SimaiProcessor.timinglist.Count <= 0) return;
        timingList.Sort((x, y) => Math.Abs(time - x.time).CompareTo(Math.Abs(time - y.time)));
        var theNote = timingList[0];
        timingList.Clear();
        timingList.AddRange(SimaiProcessor.timinglist);
        var indexOfTheNote = timingList.IndexOf(theNote);
        var pointer = FumenContent.Document.Blocks.ToList()[theNote.rawTextPositionY].ContentStart
            .GetPositionAtOffset(theNote.rawTextPositionX);
        FumenContent.Selection.Select(pointer, pointer);
    }

    private void SeekTextFromIndex(int noteGroupIndex)
    {
        if (SimaiProcessor.notelist.Count > noteGroupIndex + 1 && noteGroupIndex >= 0)
        {
            var theNote = SimaiProcessor.notelist[noteGroupIndex];
            var pointer = FumenContent.Document.Blocks.ToList()[theNote.rawTextPositionY].ContentStart
                .GetPositionAtOffset(theNote.rawTextPositionX);
            FumenContent.Selection.Select(pointer, pointer);
        }
    }

    public void ScrollToFumenContentSelection(int positionX, int positionY)
    {
        // 这玩意用于其他窗口来滚动Scroll 因为涉及到好多变量都是private的
        var pointer = FumenContent.Document.Blocks.ToList()[positionY].ContentStart.GetPositionAtOffset(positionX);
        FumenContent.Focus();
        FumenContent.Selection.Select(pointer, pointer);
        Focus();
        
        if (AudioManager.ChannelIsPlaying(ChannelType.BGM) && (bool)FollowPlayCheck.IsChecked!)
            return;
        var time = SimaiProcessor.Serialize(GetRawFumenText(), GetRawFumenPosition());
        SetBgmPosition(time);
        //Console.WriteLine("SelectionChanged");
        SimaiProcessor.ClearNoteListPlayedState();
        ghostCusorPositionTime = (float)time;
    }

    //*FIND AND REPLACE
    private void Find_icon_MouseDown(object? sender, MouseButtonEventArgs e)
    {
        FindAndScroll();
    }

    private void Replace_icon_MouseDown(object? sender, MouseButtonEventArgs e)
    {
        if (!isReplaceConformed)
        {
            FindAndScroll();
            return;
        }

        if (FumenContent.Selection == lastFindPosition)
        {
            FumenContent.Selection.Text = ReplaceText.Text;
            FindAndScroll();
        }
        else
        {
            isReplaceConformed = false;
        }
    }

    public TextRange? GetTextRangeFromPosition(TextPointer position, string input)
    {
        TextRange? textRange = null;

        while (position != null)
        {
            if (position.CompareTo(FumenContent.Document.ContentEnd) == 0) break;

            if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
            {
                var textRun = position.GetTextInRun(LogicalDirection.Forward);
                var stringComparison = StringComparison.CurrentCultureIgnoreCase;
                var indexInRun = textRun.IndexOf(input, stringComparison);

                if (indexInRun >= 0)
                {
                    position = position.GetPositionAtOffset(indexInRun);
                    var nextPointer = position.GetPositionAtOffset(input.Length);
                    textRange = new TextRange(position, nextPointer);

                    // If a none-WholeWord match is found, directly terminate the loop.
                    position = position.GetPositionAtOffset(input.Length);
                    break;
                }

                // If a match is not found, go over to the next context position after the "textRun".
                position = position.GetPositionAtOffset(textRun.Length);
            }
            else
            {
                //If the current position doesn't represent a text context position, go to the next context position.
                // This can effectively ignore the formatting or embed element symbols.
                position = position.GetNextContextPosition(LogicalDirection.Forward);
            }
        }

        return textRange;
    }

    public void FindAndScroll()
    {
        var position = GetTextRangeFromPosition(FumenContent.CaretPosition, InputText.Text);
        if (position == null)
        {
            isReplaceConformed = false;
            return;
        }

        FumenContent.Selection.Select(position.Start, position.End);
        lastFindPosition = FumenContent.Selection;
        FumenContent.Focus();
        isReplaceConformed = true;
    }

    //*FILE CONTROL
    private async Task initFromFile(string path) //file name should not be included in path
    {
        if (soundSetting != null) soundSetting.Close();
        if (editorSetting == null) ReadEditorSetting();

        var useOgg = File.Exists(path + "/track.ogg");

        var audioPath = path + "/track" + (useOgg ? ".ogg" : ".mp3");
        var dataPath = path + "/maidata.txt";
        if (!File.Exists(audioPath))
        {
            MessageBox.Show(GetLocalizedString("NoTrack"), GetLocalizedString("Error"));
            return;
        }

        if (!File.Exists(dataPath))
        {
            MessageBox.Show(GetLocalizedString("NoMaidata_txt"), GetLocalizedString("Error"));
            return;
        }

        MaidataDir = path;
        SafeTerminationDetector.Of().ChangePath(MaidataDir);
        SetRawFumenText("");
        if (!AudioManager.IsInvaildChannel(ChannelType.BGM))
            AudioManager.DisposalChannel(ChannelType.BGM);


        var info = AudioManager.LoadBGM(audioPath);
        if (info?.freq != 44100) 
            MessageBox.Show(GetLocalizedString("Warn44100Hz"), GetLocalizedString("Attention"));
        ReadWaveFromFile();
        SimaiProcessor.ClearData();

        if (!SimaiProcessor.ReadData(dataPath)) 
            return;


        LevelSelector.SelectedItem = LevelSelector.Items[0];
        await ReadSetting();
        SetRawFumenText(SimaiProcessor.fumens[selectedDifficulty]);
        SeekTextFromTime();
        SimaiProcessor.Serialize(GetRawFumenText());
        FumenContent.Focus();
        await DrawWave();

        OffsetTextBox.Text = SimaiProcessor.first.ToString();

        Cover.Visibility = Visibility.Collapsed;
        MenuEdit.IsEnabled = true;
        VolumnSetting.IsEnabled = true;
        MenuMuriCheck.IsEnabled = true;
        Menu_ExportRender.IsEnabled = true;
        SyntaxCheckButton.IsEnabled = true;
        AutoSaveManager.Of().SetAutoSaveEnable(true);
        SetSavedState(true);
        SyntaxCheck();
    }

    internal async void SyntaxCheck()
    {
        if (editorSetting!.SyntaxCheckLevel == 0)
        {
            SetErrCount(GetLocalizedString("SyntaxCheckLevel1"));
            return;
        }
#if DEBUG
        await SyntaxChecker.ScanAsync(GetRawFumenText());
        SetErrCount(SyntaxChecker.GetErrorCount());
#else
        try
        {
            await SyntaxChecker.ScanAsync(GetRawFumenText());
            SetErrCount(SyntaxChecker.ErrorList.Count);
        }
        catch
        {
            SetErrCount(GetLocalizedString("InternalErr"));
        }
#endif

    }
    void SetErrCount<T>(T eCount) => Dispatcher.Invoke(() => ErrCount.Content = $"{eCount}");
    private void ReadWaveFromFile()
    {
        var useOgg = File.Exists(MaidataDir + "/track.ogg");
        var bgmDecode = Bass.BASS_StreamCreateFile(MaidataDir + "/track" + (useOgg ? ".ogg" : ".mp3"), 0L, 0L, BASSFlag.BASS_STREAM_DECODE);
        try
        {
            songLength = Bass.BASS_ChannelBytes2Seconds(bgmDecode,
                Bass.BASS_ChannelGetLength(bgmDecode, BASSMode.BASS_POS_BYTE));
/*                int sampleNumber = (int)((songLength * 1000) / (0.02f * 1000));
                wavedBs = new float[sampleNumber];
                for (int i = 0; i < sampleNumber; i++)
                {
                    wavedBs[i] = Bass.BASS_ChannelGetLevels(bgmDecode, 0.02f, BASSLevel.BASS_LEVEL_MONO)[0];
                }*/
            Bass.BASS_StreamFree(bgmDecode);
            var bgmSample = Bass.BASS_SampleLoad(MaidataDir + "/track" + (useOgg ? ".ogg" : ".mp3"), 0, 0, 1, BASSFlag.BASS_DEFAULT);
            var bgmInfo = Bass.BASS_SampleGetInfo(bgmSample);
            var freq = bgmInfo.freq;
            var sampleCount = (long)(songLength * freq * 2);
            var bgmRAW = new short[sampleCount];
            Bass.BASS_SampleGetData(bgmSample, bgmRAW);

            waveRaws[0] = new short[sampleCount / 20 + 1];
            for (var i = 0; i < sampleCount; i = i + 20) waveRaws[0][i / 20] = bgmRAW[i];
            waveRaws[1] = new short[sampleCount / 50 + 1];
            for (var i = 0; i < sampleCount; i = i + 50) waveRaws[1][i / 50] = bgmRAW[i];
            waveRaws[2] = new short[sampleCount / 100 + 1];
            for (var i = 0; i < sampleCount; i = i + 100) waveRaws[2][i / 100] = bgmRAW[i];
        }
        catch (Exception e)
        {
            MessageBox.Show("mp3/ogg解码失败。\nMP3/OGG Decode fail.\n" + e.Message + Bass.BASS_ErrorGetCode());
            Bass.BASS_StreamFree(bgmDecode);
            Process.Start("https://github.com/LingFeng-bbben/MajdataEdit/issues/26");
        }
    }

    private void SetSavedState(bool state)
    {
        if (state)
        {
            isSaved = true;
            LevelSelector.IsEnabled = true;
            TheWindow.Title = GetWindowsTitleString(SimaiProcessor.title!);
        }
        else
        {
            isSaved = false;
            LevelSelector.IsEnabled = false;
            TheWindow.Title = GetWindowsTitleString(GetLocalizedString("Unsaved") + SimaiProcessor.title!);
            AutoSaveManager.Of().SetFileChanged();
        }
    }

    /// <summary>
    ///     Ask the user and save fumen.
    /// </summary>
    /// <returns>Return false if user cancel the action</returns>
    private bool AskSave()
    {
        var result = MessageBox.Show(GetLocalizedString("AskSave"), GetLocalizedString("Warning"),
            MessageBoxButton.YesNoCancel);
        if (result == MessageBoxResult.Yes)
        {
            SaveFumen(true);
            return true;
        }

        if (result == MessageBoxResult.Cancel) return false;
        return true;
    }

    private void SaveFumen(bool writeToDisk = false)
    {
        if (selectedDifficulty == -1) return;
        SimaiProcessor.fumens[selectedDifficulty] = GetRawFumenText();
        SimaiProcessor.first = float.Parse(OffsetTextBox.Text);
        if (MaidataDir == "")
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "maidata.txt|maidata.txt",
                OverwritePrompt = true
            };
            if ((bool)saveDialog.ShowDialog()!) MaidataDir = new FileInfo(saveDialog.FileName).DirectoryName!;
        }

        SimaiProcessor.SaveData(MaidataDir + "/maidata.bak.txt");
        SaveSetting();
        if (writeToDisk)
        {
            SimaiProcessor.SaveData(MaidataDir + "/maidata.txt");
            SetSavedState(true);
        }
    }

    private async Task SaveSetting()
    {
        if (MaidataDir == "") 
            return;

        var path = Path.Combine(MaidataDir, majSettingFilename);
        var setting = new MajSetting
        {
            LastEditDiff = selectedDifficulty,
            LastEditTime = AudioManager.GetSeconds(ChannelType.BGM)
        };
        AudioManager.SaveSetting(setting);
        using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, setting);
    }

    private async Task ReadSetting()
    {
        var path = Path.Combine(MaidataDir, majSettingFilename);
        if (!File.Exists(path)) 
            return;

        using var stream = File.OpenRead(path);
        var setting = await JsonSerializer.DeserializeAsync<MajSetting>(stream);

        LevelSelector.SelectedIndex = setting!.LastEditDiff;
        selectedDifficulty = setting.LastEditDiff;
        SetBgmPosition(setting.LastEditTime);

        AudioManager.ReadSetting(setting);

        await SaveSetting(); // 覆盖旧版本setting
    }

    private void CreateNewFumen(string path)
    {
        if (File.Exists(path + "/maidata.txt"))
            MessageBox.Show(GetLocalizedString("MaidataExist"));
        else
            File.WriteAllText(path + "/maidata.txt",
                "&title=" + GetLocalizedString("SetTitle") + "\n" +
                "&artist=" + GetLocalizedString("SetArtist") + "\n" +
                "&des=" + GetLocalizedString("SetDes") + "\n" +
                "&first=0\n");
    }

    private void CreateEditorSetting()
    {
        editorSetting = new EditorSetting
        {
            RenderMode =
            RenderOptions.ProcessRenderMode == RenderMode.SoftwareOnly ? RenderType.SW : RenderType.HW // 使用命令行指定强制软件渲染时，同步修改配置值
        };

        File.WriteAllText(editorSettingFilename, JsonConvert.SerializeObject(editorSetting, Formatting.Indented));

        var esp = new EditorSettingPanel(true)
        {
            Owner = this
        };
        esp.ShowDialog();
    }

    private void ReadEditorSetting()
    {
        if (!File.Exists(editorSettingFilename)) CreateEditorSetting();
        var json = File.ReadAllText(editorSettingFilename);
        editorSetting = JsonConvert.DeserializeObject<EditorSetting>(json)!;

        if (RenderOptions.ProcessRenderMode != RenderMode.SoftwareOnly)
            //如果没有通过命令行预先指定渲染模式，则使用设置项的渲染模式
            RenderOptions.ProcessRenderMode =
                editorSetting.RenderMode == 0 ? RenderMode.Default : RenderMode.SoftwareOnly;
        else
            //如果通过命令行指定了使用软件渲染模式，则覆盖设置项
            editorSetting.RenderMode = RenderType.SW;

        LocalizeDictionary.Instance.Culture = new CultureInfo(editorSetting.Language);
        AddGesture(editorSetting.PlayPauseKey, "PlayAndPause");
        AddGesture(editorSetting.PlayStopKey, "StopPlaying");
        AddGesture(editorSetting.SaveKey, "SaveFile");
        AddGesture(editorSetting.SendViewerKey, "SendToView");
        AddGesture(editorSetting.IncreasePlaybackSpeedKey, "IncreasePlaybackSpeed");
        AddGesture(editorSetting.DecreasePlaybackSpeedKey, "DecreasePlaybackSpeed");
        AddGesture("Ctrl+f", "Find");
        AddGesture(editorSetting.MirrorLeftRightKey, "MirrorLR");
        AddGesture(editorSetting.MirrorUpDownKey, "MirrorUD");
        AddGesture(editorSetting.Mirror180Key, "Mirror180");
        AddGesture(editorSetting.Mirror45Key, "Mirror45");
        AddGesture(editorSetting.MirrorCcw45Key, "MirrorCcw45");
        FumenContent.FontSize = editorSetting.FontSize;

        ViewerCover.Content = editorSetting.backgroundCover.ToString();
        ViewerSpeed.Content = editorSetting.NoteSpeed.ToString("F1"); // 转化为形如"7.0", "9.5"这样的速度
        ViewerTouchSpeed.Content = editorSetting.TouchSpeed.ToString("F1");

        chartChangeTimer.Interval = editorSetting.ChartRefreshDelay; // 设置更新延迟

        SaveEditorSetting(); // 覆盖旧版本setting
    }

    public void SaveEditorSetting()
    {
        File.WriteAllText(editorSettingFilename, JsonConvert.SerializeObject(editorSetting, Formatting.Indented));
    }

    private void AddGesture(string keyGusture, string command)
    {
        var gesture = (InputGesture) new KeyGestureConverter().ConvertFromString(keyGusture)!;
        var inputBinding = new InputBinding((ICommand)FumenContent.Resources[command], gesture);
        FumenContent.InputBindings.Add(inputBinding);
    }

    // This update very freqently to Draw FFT wave.
    private async void VisualEffectRefreshTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            await DrawFFT();
            await DrawWave();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    // 谱面变更延迟解析
    private async void ChartChangeTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        Console.WriteLine("TextChanged");
        SyntaxCheck();
        await Dispatcher.InvokeAsync(async () => 
        {
            SimaiProcessor.Serialize(GetRawFumenText(), GetRawFumenPosition());
            await DrawWave();
        });
    }

    private async Task DrawFFT()
    {
        await Dispatcher.InvokeAsync(() =>
        {
            //Scroll WaveView
            var currentTime = AudioManager.GetSeconds(ChannelType.BGM);
            //MusicWave.Margin = new Thickness(-currentTime / sampleTime * zoominPower, Margin.Left, MusicWave.Margin.Right, Margin.Bottom);
            //MusicWaveCusor.Margin = new Thickness(-currentTime / sampleTime * zoominPower, Margin.Left, MusicWave.Margin.Right, Margin.Bottom);

            var writableBitmap = new WriteableBitmap(255, 255, 72, 72, PixelFormats.Pbgra32, null);
            FFTImage.Source = writableBitmap;
            writableBitmap.Lock();
            var backBitmap = new Bitmap(255, 255, writableBitmap.BackBufferStride,
                PixelFormat.Format32bppArgb, writableBitmap.BackBuffer);

            var graphics = Graphics.FromImage(backBitmap);
            graphics.Clear(Color.Transparent);

            var fft = new float[1024];
            AudioManager.GetChannelData(ChannelType.BGM,ref fft, (int)BASSData.BASS_DATA_FFT1024);
            var points = new PointF[1024];
            for (var i = 0; i < fft.Length; i++)
                points[i] = new PointF((float)Math.Log10(i + 1) * 100f, 240 - fft[i] * 256); //semilog

            graphics.DrawCurve(new Pen(Color.LightSkyBlue, 1), points);


            //no please
            /*
            var isSuccess = new Visuals().CreateSpectrumWave(bgmStream, graphics, new System.Drawing.Rectangle(0, 0, 255, 255),
                System.Drawing.Color.White, System.Drawing.Color.Red,
                System.Drawing.Color.Black, 1,
                false, false, false);
            Console.WriteLine(isSuccess);
            */
            graphics.Flush();
            graphics.Dispose();
            backBitmap.Dispose();

            writableBitmap.AddDirtyRect(new Int32Rect(0, 0, 255, 255));
            writableBitmap.Unlock();
        });
    }

    private void InitWave()
    {
        var width = (int)Width - 2;
        var height = (int)MusicWave.Height;
        WaveBitmap = new WriteableBitmap(width, height, 72, 72, PixelFormats.Pbgra32, null);
        MusicWave.Source = WaveBitmap;
    }

    private async Task DrawWave()
    {
        if (isDrawing) return;
        if (WaveBitmap == null) return;

        await Dispatcher.InvokeAsync(() =>
        {
            isDrawing = true;
            var width = WaveBitmap.PixelWidth;
            var height = WaveBitmap.PixelHeight;

            if (waveRaws[0] == null)
            {
                isDrawing = false;
                return;
            }

            WaveBitmap.Lock();

            //the process starts
            var backBitmap = new Bitmap(width, height, WaveBitmap.BackBufferStride,
                PixelFormat.Format32bppArgb, WaveBitmap.BackBuffer);
            var graphics = Graphics.FromImage(backBitmap);
            var currentTime = AudioManager.GetSeconds(ChannelType.BGM);

            graphics.Clear(Color.FromArgb(100, 0, 0, 0));

            var resample = (int)deltatime - 1;
            if (resample > 1 && resample <= 3) resample = 1;
            if (resample > 3) resample = 2;
            var waveLevels = waveRaws[resample];

            var step = songLength / waveLevels.Length;
            var startindex = (int)((currentTime - deltatime) / step);
            var stopindex = (int)((currentTime + deltatime) / step);
            var linewidth = backBitmap.Width / (float)(stopindex - startindex);
            var pen = new Pen(Color.Green, linewidth);
            var points = new List<PointF>();
            for (var i = startindex; i < stopindex; i = i + 1)
            {
                if (i < 0) i = 0;
                if (i >= waveLevels.Length - 1) break;

                var x = (i - startindex) * linewidth;
                var y = waveLevels[i] / 65535f * height + height / 2;

                points.Add(new PointF(x, y));
            }

            graphics.DrawLines(pen, points.ToArray());

            //Draw Bpm lines
            var lastbpm = -1f;
            var bpmChangeTimes = new List<double>(); //在什么时间变成什么值
            var bpmChangeValues = new List<float>();
            bpmChangeTimes.Clear();
            bpmChangeValues.Clear();
            foreach (var timing in SimaiProcessor.timinglist)
                if (timing.currentBpm != lastbpm)
                {
                    bpmChangeTimes.Add(timing.time);
                    bpmChangeValues.Add(timing.currentBpm);
                    lastbpm = timing.currentBpm;
                }

            bpmChangeTimes.Add(AudioManager.GetSeconds(ChannelType.BGM));

            double time = SimaiProcessor.first;
            var signature = 4; //预留拍号
            var currentBeat = 1;
            var timePerBeat = 0d;
            pen = new Pen(Color.Yellow, 1);
            var strongBeat = new List<double>();
            var weakBeat = new List<double>();
            for (var i = 1; i < bpmChangeTimes.Count; i++)
            {
                while (time - bpmChangeTimes[i] < -0.05) //在那个时间之前都是之前的bpm
                {
                    if (currentBeat > signature) currentBeat = 1;
                    timePerBeat = 1d / (bpmChangeValues[i - 1] / 60d);
                    if (currentBeat == 1)
                        strongBeat.Add(time);
                    else
                        weakBeat.Add(time);
                    currentBeat++;
                    time += timePerBeat;
                }

                time = bpmChangeTimes[i];
                currentBeat = 1;
            }

            foreach (var btime in strongBeat)
            {
                if (btime - currentTime > deltatime) continue;
                var x = ((float)(btime / step) - startindex) * linewidth;
                graphics.DrawLine(pen, x, 0, x, 75);
            }

            foreach (var btime in weakBeat)
            {
                if (btime - currentTime > deltatime) continue;
                var x = ((float)(btime / step) - startindex) * linewidth;
                graphics.DrawLine(pen, x, 0, x, 15);
            }

            //Draw timing lines
            pen = new Pen(Color.White, 1);
            foreach (var note in SimaiProcessor.timinglist)
            {
                if (note == null) break;
                if (note.time - currentTime > deltatime) continue;
                var x = ((float)(note.time / step) - startindex) * linewidth;
                graphics.DrawLine(pen, x, 60, x, 75);
            }

            //Draw notes                    
            foreach (var note in SimaiProcessor.notelist)
            {
                if (note == null) break;
                if (note.time - currentTime > deltatime) continue;
                var notes = note.getNotes();
                var isEach = notes.Count(o => !o.isSlideNoHead) > 1;

                var x = ((float)(note.time / step) - startindex) * linewidth;

                foreach (var noteD in notes)
                {
                    var y = noteD.startPosition * 6.875f + 8f; //与键位有关

                    if (noteD.isHanabi)
                    {
                        var xDeltaHanabi = (float)(1f / step) * linewidth; //Hanabi is 1s due to frame analyze
                        var rectangleF = new RectangleF(x, 0, xDeltaHanabi, 75);
                        if (noteD.noteType == SimaiNoteType.TouchHold)
                            rectangleF.X += (float)(noteD.holdTime / step) * linewidth;
                        var gradientBrush = new LinearGradientBrush(
                            rectangleF,
                            Color.FromArgb(100, 255, 0, 0),
                            Color.FromArgb(0, 255, 0, 0),
                            LinearGradientMode.Horizontal
                        );
                        graphics.FillRectangle(gradientBrush, rectangleF);
                    }

                    if (noteD.noteType == SimaiNoteType.Tap)
                    {
                        if (noteD.isForceStar)
                        {
                            pen.Width = 3;
                            if (noteD.isBreak)
                                pen.Color = Color.OrangeRed;
                            else if (isEach)
                                pen.Color = Color.Gold;
                            else
                                pen.Color = Color.DeepSkyBlue;
                            Brush brush = new SolidBrush(pen.Color);
                            graphics.DrawString("*", new Font("Consolas", 12, System.Drawing.FontStyle.Bold), brush,
                                new PointF(x - 7f, y - 7f));
                        }
                        else
                        {
                            pen.Width = 2;
                            if (noteD.isBreak)
                                pen.Color = Color.OrangeRed;
                            else if (isEach)
                                pen.Color = Color.Gold;
                            else
                                pen.Color = Color.LightPink;
                            graphics.DrawEllipse(pen, x - 2.5f, y - 2.5f, 5, 5);
                        }
                    }

                    if (noteD.noteType == SimaiNoteType.Touch)
                    {
                        pen.Width = 2;
                        pen.Color = isEach ? Color.Gold : Color.DeepSkyBlue;
                        graphics.DrawRectangle(pen, x - 2.5f, y - 2.5f, 5, 5);
                    }

                    if (noteD.noteType == SimaiNoteType.Hold)
                    {
                        pen.Width = 3;
                        if (noteD.isBreak)
                            pen.Color = Color.OrangeRed;
                        else if (isEach)
                            pen.Color = Color.Gold;
                        else
                            pen.Color = Color.LightPink;

                        var xRight = x + (float)(noteD.holdTime / step) * linewidth;

                        //1h[0:1]
                        if (!float.IsNormal(xRight)) xRight = ushort.MaxValue;
                        if (xRight - x < 1f) xRight = x + 5;
                        graphics.DrawLine(pen, x, y, xRight, y);

                    }

                    if (noteD.noteType == SimaiNoteType.TouchHold)
                    {
                        pen.Width = 3;
                        var xDelta = (float)(noteD.holdTime / step) * linewidth / 4f;
                        //Console.WriteLine("HoldPixel"+ xDelta);
                        if (!float.IsNormal(xDelta)) xDelta = ushort.MaxValue;
                        if (xDelta < 1f) xDelta = 1;

                        pen.Color = Color.FromArgb(200, 255, 75, 0);
                        graphics.DrawLine(pen, x, y, x + xDelta * 4f, y);
                        pen.Color = Color.FromArgb(200, 255, 241, 0);
                        graphics.DrawLine(pen, x, y, x + xDelta * 3f, y);
                        pen.Color = Color.FromArgb(200, 2, 165, 89);
                        graphics.DrawLine(pen, x, y, x + xDelta * 2f, y);
                        pen.Color = Color.FromArgb(200, 0, 140, 254);
                        graphics.DrawLine(pen, x, y, x + xDelta, y);
                    }

                    if (noteD.noteType == SimaiNoteType.Slide)
                    {
                        pen.Width = 3;
                        if (!noteD.isSlideNoHead)
                        {
                            if (noteD.isBreak)
                                pen.Color = Color.OrangeRed;
                            else if (isEach)
                                pen.Color = Color.Gold;
                            else
                                pen.Color = Color.DeepSkyBlue;
                            Brush brush = new SolidBrush(pen.Color);
                            graphics.DrawString("*", new Font("Consolas", 12, System.Drawing.FontStyle.Bold), brush,
                                new PointF(x - 7f, y - 7f));
                        }

                        if (noteD.isSlideBreak)
                            pen.Color = Color.OrangeRed;
                        else if (notes.Count(o => o.noteType == SimaiNoteType.Slide) >= 2)
                            pen.Color = Color.Gold;
                        else
                            pen.Color = Color.SkyBlue;
                        pen.DashStyle = DashStyle.Dot;
                        var xSlide = (float)(noteD.slideStartTime / step - startindex) * linewidth;
                        var xSlideRight = (float)(noteD.slideTime / step) * linewidth + xSlide;

                        if (!float.IsNormal(xSlideRight)) xSlideRight = ushort.MaxValue;
                        if (!float.IsNormal(xSlide)) xSlide = ushort.MaxValue;

                        graphics.DrawLine(pen, xSlide, y, xSlideRight, y);
                        pen.DashStyle = DashStyle.Solid;
                    }
                }
            }

            if (playStartTime - currentTime <= deltatime)
            {
                //Draw play Start time
                pen = new Pen(Color.Red, 5);
                var x1 = (float)(playStartTime / step - startindex) * linewidth;
                PointF[] tranglePoints = { new(x1 - 2, 0), new(x1 + 2, 0), new(x1, 3.46f) };
                graphics.DrawPolygon(pen, tranglePoints);
            }

            if (ghostCusorPositionTime - currentTime <= deltatime)
            {
                //Draw ghost cusor
                pen = new Pen(Color.Orange, 5);
                var x2 = (float)(ghostCusorPositionTime / step - startindex) * linewidth;
                PointF[] tranglePoints2 = { new(x2 - 2, 0), new(x2 + 2, 0), new(x2, 3.46f) };
                graphics.DrawPolygon(pen, tranglePoints2);
            }

            graphics.Flush();
            graphics.Dispose();
            backBitmap.Dispose();

            //MusicWave.Width = waveLevels.Length * zoominPower;
            WaveBitmap.AddDirtyRect(new Int32Rect(0, 0, WaveBitmap.PixelWidth, WaveBitmap.PixelHeight));
            WaveBitmap.Unlock();
            isDrawing = false;
        });
    }

    // This update less frequently. set the time text.
    private void CurrentTimeRefreshTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        UpdateTimeDisplay();
    }

    private void UpdateTimeDisplay()
    {
        var currentPlayTime = AudioManager.GetSeconds(ChannelType.BGM);
        var minute = (int)currentPlayTime / 60;
        double second = (int)(currentPlayTime - 60 * minute);
        Dispatcher.Invoke(() => { TimeLabel.Content = string.Format("{0}:{1:00}", minute, second); });
    }

    private async Task ScrollWave(double delta)
    {
        if (AudioManager.ChannelIsPlaying(ChannelType.BGM))
            await TogglePause();
        delta = delta * deltatime / (Width / 2);
        var time = AudioManager.GetSeconds(ChannelType.BGM);
        SetBgmPosition(time + delta);
        SimaiProcessor.ClearNoteListPlayedState();
        SeekTextFromTime();
        await DrawWave();
    }

    public static string GetLocalizedString(string key, string resourceFileName = "Langs", bool addSpaceAfter = false)
    {

        // Build up the fully-qualified name of the key

        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        var fullKey = assemblyName + ":" + resourceFileName + ":" + key;
        var locExtension = new LocExtension(fullKey);
        locExtension.ResolveLocalizedValue(out string? localizedString);

        // Add a space to the end, if requested
        if (addSpaceAfter) localizedString += " ";

        return localizedString ?? key;
    }

    

    

    private void SetPlaybackSpeed(float speed)
    {
        var scale = (speed - 1) * 100f;
        AudioManager.SetPlaybackSpeed(ChannelType.BGM, scale);
    }

    private float GetPlaybackSpeed()
    {
        var speed = 0f;
        AudioManager.GetPlaybackSpeed(ChannelType.BGM, ref speed);
        return speed / 100f + 1f;
    }

    private async Task SetBgmPosition(double time)
    {
        if (EditorState == EditorControlMethod.Pause) 
            await RequestToStop();
        AudioManager.SetSeconds(ChannelType.BGM, time);
    }


    
    
    private void InternalSwitchWindow(bool moveToPlace = true)
    {
        var windowPtr = FindWindow(null, "MajdataView");
        //var thisWindow = FindWindow(null, this.Title);
        ShowWindow(windowPtr, 5); //还原窗口
        SwitchToThisWindow(windowPtr, true);
        //SwitchToThisWindow(thisWindow, true);
        if (moveToPlace) InternalMoveWindow();
    }
    private void InternalMoveWindow()
    {
        var windowPtr = FindWindow(null, "MajdataView");
        var source = PresentationSource.FromVisual(this);

        double dpiX = 1, dpiY = 1;
        if (source != null)
        {
            dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
            dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
        }

        //Console.WriteLine(dpiX+" "+dpiY);
        dpiX /= 96d;
        dpiY /= 96d;

        var Height = this.Height * dpiY;
        var Left = this.Left * dpiX;
        var Top = this.Top * dpiY;
        MoveWindow(windowPtr,
            (int)(Left - Height + 20),
            (int)Top,
            (int)Height - 20,
            (int)Height, true);
    }
    /// <summary>
    /// 窗口位置设置
    /// </summary>
    private void SetWindowGoldenPosition()
    {
        // 属于你的独享黄金位置
        var ScreenWidth = SystemParameters.PrimaryScreenWidth;
        var ScreenHeight = SystemParameters.PrimaryScreenHeight;

        Left = (ScreenWidth - Width + Height) / 2 - 10;
        Top = (ScreenHeight - Height) / 2;
    }
    /// <summary>
    /// 富文本控件输入模式切换
    /// </summary>
    private void SwitchFumenOverwriteMode()
    {
        fumenOverwriteMode = !fumenOverwriteMode;

        //修改覆盖模式启用状态
        // fetch TextEditor from FumenContent
        var textEditorProperty =
            typeof(TextBox).GetProperty("TextEditor", BindingFlags.NonPublic | BindingFlags.Instance);
        var textEditor = textEditorProperty!.GetValue(FumenContent, null);

        // set _OvertypeMode on the TextEditor
        var overtypeModeProperty = textEditor!.GetType()
            .GetProperty("_OvertypeMode", BindingFlags.NonPublic | BindingFlags.Instance)!;
        overtypeModeProperty!.SetValue(textEditor, fumenOverwriteMode, null);

        //修改提示弹窗可见性
        OverrideModeTipsPopup.Visibility = fumenOverwriteMode ? Visibility.Visible : Visibility.Collapsed;
    }
    /// <summary>
    /// Majdata检查更新
    /// </summary>
    /// <param name="onStart"></param>
    /// <returns></returns>
    private async Task CheckUpdate(bool onStart = false)
    {
        if (UpdateCheckLock) return;
        UpdateCheckLock = true;

        #region 子函数

        SemVersion oldVersionCompatible(string versionString)
        {
            var result = SemVersion.Parse("v0.0.0", SemVersionStyles.Any);
            try
            {
                // 尝试解析版本号，解析失败说明是旧版本格式
                result = SemVersion.Parse(versionString, SemVersionStyles.Any);
            }
            catch (FormatException)
            {
                if (versionString.Contains("Back2Root"))
                {
                    // back to root特别版本
                    result = SemVersion.Parse("v0.0.0", SemVersionStyles.Any);
                }
                else if (versionString.Contains("Early Access"))
                {
                    // EA版本
                    result = SemVersion.Parse("v0.0.1", SemVersionStyles.Any);
                }
                else if (versionString.Contains("Alpha"))
                {
                    // 旧版本格式 Alpha<MainVersion>.<SubVersion>[.<ModifiedVersion>]
                    // 从4.0开始，结束于6.4
                    // 在原版本号基础上增加 0. 主版本前缀，并增加 -alpha 后缀
                    var startPos = versionString.IndexOfAny("0123456789".ToArray());
                    versionString = "0." + versionString[startPos..];
                    if (versionString.Count(c => { return c == '.'; }) > 2)
                        versionString = versionString[..versionString.LastIndexOf('.')];
                    versionString += "-alpha";
                    result = SemVersion.Parse(versionString, SemVersionStyles.Any);
                }
                else if (versionString.Contains("Beta"))
                {
                    // 旧版本格式 Beta<MainVersion>.<SubVersion>[.<ModifiedVersion>]
                    // 从1.0开始，结束于3.1。后续的语义化版本号继承该版本号进度，从4.0开始
                    // 增加 -beta 后缀
                    var startPos = versionString.IndexOfAny("0123456789".ToArray());
                    versionString = versionString[startPos..];
                    if (versionString.Contains(' '))
                        versionString = versionString[..versionString.IndexOf(' ')];
                    versionString += "-beta";
                    result = SemVersion.Parse(versionString, SemVersionStyles.Any);
                }
                else
                {
                    // 其他无法识别的版本，均设置为v0.0.1-unknown
                    result = SemVersion.Parse("v0.0.1-unknown", SemVersionStyles.Any);
                }
            }

            return result;
        }

        #endregion

        // 检查是否需要更新软件

        try
        {
            var rsp =  await WebControl.RequestGETAsync("http://api.github.com/repos/LingFeng-bbben/MajdataView/releases/latest");

            UpdateCheckLock = false;
            var resJson = Serializer.Json.Deserialize<Dictionary<string, object>>(rsp)!;
            var latestVersionString = resJson["tag_name"]!.ToString() ?? string.Empty;
            var releaseUrl = resJson["html_url"]!.ToString();

            var latestVersion = oldVersionCompatible(latestVersionString);

            if (latestVersion.ComparePrecedenceTo(MAJDATA_VERSION) > 0)
            {
                // 版本不同，需要更新
                var msgboxText = string.Format(GetLocalizedString("NewVersionDetected"), latestVersionString,
                    MAJDATA_VERSION_STRING);
                if (onStart) msgboxText += "\n\n" + GetLocalizedString("AutoUpdateCheckTip");

                var result = MessageBox.Show(
                    msgboxText,
                    GetLocalizedString("CheckUpdate"),
                    MessageBoxButton.YesNo);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        var startInfo = new ProcessStartInfo(releaseUrl)
                        {
                            UseShellExecute = true
                        };
                        Process.Start(startInfo);
                        break;
                    case MessageBoxResult.No:
                        break;
                }
            }
            else
            {
                // 没有新版本，可以不用更新
                if (!onStart) MessageBox.Show(GetLocalizedString("NoNewVersion"), GetLocalizedString("CheckUpdate"));
            }
        } 
        catch (Exception e)
        {
            // 网络请求失败
            if (!onStart) MessageBox.Show(GetLocalizedString("RequestFail"), GetLocalizedString("CheckUpdate"));
        }
    }
    /// <summary>
    /// 获取带版本号的窗体标题
    /// </summary>
    /// <returns></returns>
    public string GetWindowsTitleString() => $"MajdataEdit ({MAJDATA_VERSION_STRING})";
    /// <summary>
    /// 获取带版本号的窗体标题
    /// </summary>
    /// <returns></returns>
    public string GetWindowsTitleString(string info)
    {
        try
        {
            var details = "Editing: " + SimaiProcessor.title;
            if (details.Length > 50)
                details = details[..50];
            DCRPCclient.SetPresence(new RichPresence
            {
                Details = details,
                State = "With note count of " + SimaiProcessor.notelist.Count,
                Assets = new Assets
                {
                    LargeImageKey = "salt",
                    LargeImageText = "Majdata",
                    SmallImageKey = "None"
                }
            });
        }
        catch
        {
        }

        return GetWindowsTitleString() + " - " + info;
    }

    public void OpenFile(string path)
    {
        initFromFile(path);
    }
}
