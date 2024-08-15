using MajdataEdit.Types;
using MajdataEdit.Utils;
using System.ComponentModel;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using Un4seen.Bass;
using Timer = System.Timers.Timer;

namespace MajdataEdit;

/// <summary>
///     SoundSetting.xaml 的交互逻辑
/// </summary>
public partial class SoundSetting : Window
{
    private readonly MainWindow MainWindow;
    private readonly Dictionary<Slider, Label> SliderValueBindingMap = new(); // Slider和ValueLabel的绑定关系

    private readonly Timer UpdateLevelTimer = new(1);

    public SoundSetting()
    {
        MainWindow = (Application.Current.Windows
            .Cast<Window>()
            .FirstOrDefault(window => window is MainWindow) as MainWindow)!;

        InitializeComponent();
    }

    private void SoundSettingWindow_Loaded(object sender, RoutedEventArgs e)
    {
        SliderValueBindingMap.Add(BGM_Slider, BGM_Value);
        SliderValueBindingMap.Add(Answer_Slider, Answer_Value);
        SliderValueBindingMap.Add(Judge_Slider, Judge_Value);
        SliderValueBindingMap.Add(Break_Slider, Break_Value);
        SliderValueBindingMap.Add(BreakSlide_Slider, BreakSlide_Value);
        SliderValueBindingMap.Add(Slide_Slider, Slide_Value);
        SliderValueBindingMap.Add(EX_Slider, EX_Value);
        SliderValueBindingMap.Add(Touch_Slider, Touch_Value);
        SliderValueBindingMap.Add(Hanabi_Slider, Hanabi_Value);

        SetSlider(BGM_Slider, ChannelType.BGM, ChannelType.TrackStart, ChannelType.APSFX, ChannelType.Clock);
        SetSlider(Answer_Slider, ChannelType.Answer);
        SetSlider(Judge_Slider, ChannelType.TapJudge);
        SetSlider(Break_Slider, ChannelType.Break, ChannelType.BreakJudge);
        SetSlider(BreakSlide_Slider, ChannelType.BreakSlideEnd, ChannelType.BreakSlideJudge);
        SetSlider(Slide_Slider, ChannelType.BreakSlideStart, ChannelType.BreakSlideStart);
        SetSlider(EX_Slider, ChannelType.ExJudge);
        SetSlider(Touch_Slider, ChannelType.Touch);
        SetSlider(Hanabi_Slider, ChannelType.Hanabi, ChannelType.HoldRiser);


        UpdateLevelTimer.AutoReset = true;
        UpdateLevelTimer.Elapsed += UpdateLevelTimer_Elapsed;
        UpdateLevelTimer.Start();
    }

    private void UpdateLevelTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateProgressBar(BGM_Level, ChannelType.BGM, ChannelType.TrackStart, ChannelType.APSFX, ChannelType.Clock);
            UpdateProgressBar(Answer_Level, ChannelType.Answer);
            UpdateProgressBar(Judge_Level, ChannelType.TapJudge);
            UpdateProgressBar(Break_Level, ChannelType.Break, ChannelType.BreakJudge);
            UpdateProgressBar(BreakSlide_Level, ChannelType.BreakSlideEnd, ChannelType.BreakSlideJudge);
            UpdateProgressBar(Slide_Level, ChannelType.BreakSlideStart, ChannelType.BreakSlideStart);
            UpdateProgressBar(EX_Level, ChannelType.ExJudge);
            UpdateProgressBar(Touch_Level, ChannelType.Touch);
            UpdateProgressBar(Hanabi_Level, ChannelType.Hanabi, ChannelType.HoldRiser);
        });
    }

    private void UpdateProgressBar(ProgressBar bar, params ChannelType[] channels)
    {
        var values = new double[channels.Length];
        var ampLevel = 0f;
        for (var i = 0; i < channels.Length; i++)
            values[i] = AudioManager.GetChannelDB(channels[i]);

        var value = values.Max();
        if (!double.IsNaN(value) && !double.IsInfinity(value)) bar.Value = value * ampLevel;
        if (double.IsNegativeInfinity(value)) bar.Value = bar.Minimum;
        if (double.IsPositiveInfinity(value)) bar.Value = bar.Maximum;
        if (double.IsNaN(value)) bar.Value -= 1;
    }

    private void SetSlider(Slider slider, params ChannelType[] channels)
    {
        var ampLevel = 0f;
        foreach (var channel in channels)
            AudioManager.GetVolume(channel,ref ampLevel);
        slider.Value = ampLevel;
        SliderValueBindingMap[slider].Content = slider.Value.ToString("P0");

        void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var sld = (Slider)sender;
            foreach (var channel in channels)
                AudioManager.SetVolume(channel, (float)sld.Value);

            SliderValueBindingMap[sld].Content = sld.Value.ToString("P0");
        }

        slider.ValueChanged += Slider_ValueChanged;
    }

    private void SoundSettingWindow_Closing(object sender, CancelEventArgs e)
    {
        UpdateLevelTimer.Stop();
        UpdateLevelTimer.Dispose();
    }

    private void BtnSetDefault_Click(object sender, RoutedEventArgs e)
    {
        AudioManager.SetVolume(ChannelType.BGM, MainWindow.editorSetting!.Default_BGM_Level);
        AudioManager.SetVolume(ChannelType.Answer, MainWindow.editorSetting!.Default_Answer_Level);
        AudioManager.SetVolume(ChannelType.TapJudge, MainWindow.editorSetting!.Default_Judge_Level);
        AudioManager.SetVolume(ChannelType.Break, MainWindow.editorSetting!.Default_Break_Level);
        AudioManager.SetVolume(ChannelType.BreakSlideEnd, MainWindow.editorSetting!.Default_Break_Slide_Level);
        AudioManager.SetVolume(ChannelType.BreakSlideStart, MainWindow.editorSetting!.Default_Slide_Level);
        AudioManager.SetVolume(ChannelType.ExJudge, MainWindow.editorSetting!.Default_Ex_Level);
        AudioManager.SetVolume(ChannelType.Touch, MainWindow.editorSetting!.Default_Touch_Level);
        AudioManager.SetVolume(ChannelType.Hanabi, MainWindow.editorSetting!.Default_Hanabi_Level);

        MainWindow.SaveEditorSetting();
        MessageBox.Show(MainWindow.GetLocalizedString("SetVolumeDefaultSuccess"));
    }

    private void BtnSetToDefault_Click(object sender, RoutedEventArgs e)
    {
        BGM_Slider.Value = MainWindow.editorSetting!.Default_BGM_Level;
        Answer_Slider.Value = MainWindow.editorSetting!.Default_Answer_Level;
        Judge_Slider.Value = MainWindow.editorSetting!.Default_Judge_Level;
        Break_Slider.Value = MainWindow.editorSetting!.Default_Break_Level;
        BreakSlide_Slider.Value = MainWindow.editorSetting!.Default_Break_Slide_Level;
        Slide_Slider.Value = MainWindow.editorSetting!.Default_Slide_Level;
        EX_Slider.Value = MainWindow.editorSetting!.Default_Ex_Level;
        Touch_Slider.Value = MainWindow.editorSetting!.Default_Touch_Level;
        Hanabi_Slider.Value = MainWindow.editorSetting!.Default_Hanabi_Level;
    }
}