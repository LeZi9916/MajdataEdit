using MajdataEdit.Types;
using MajdataEdit.Utils;
using System.Media;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using Timer = System.Timers.Timer;

namespace MajdataEdit;
public partial class MainWindow : Window
{
    private readonly Timer playbackSpeedHideTimer = new(1000);


    void SetControlButtonActive(bool isEnable)
    {
        Dispatcher.Invoke(() =>
        {
            PlayAndPauseButton.IsEnabled = isEnable;
            StopButton.IsEnabled = isEnable;
            Op_Button.IsEnabled = isEnable;
        });
    }
    private async void PlayAndPause_CanExecute(object? sender, CanExecuteRoutedEventArgs e) //快捷键
    {
        if (!PlayAndPauseButton.IsEnabled)
            return;
        await TogglePlayAndStop();
    }
    private async void StopPlaying_CanExecute(object? sender, CanExecuteRoutedEventArgs e) //快捷键
    {
        if (!PlayAndPauseButton.IsEnabled)
            return;
        await TogglePlayAndPause();
    }
    private async void SaveFile_Command_CanExecute(object? sender, CanExecuteRoutedEventArgs e)
    {
        await SaveFumen(true);
        SystemSounds.Beep.Play();
    }
    /// <summary>
    /// 录制预览
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void SendToView_CanExecute(object? sender, CanExecuteRoutedEventArgs e)
    {
        if (!Op_Button.IsEnabled)
            return;
        await TogglePlayAndStop(PlayMethod.Op);
    }
    /// <summary>
    /// 加快播放速度
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void IncreasePlaybackSpeed_CanExecute(object? sender, CanExecuteRoutedEventArgs e)
    {
        if (AudioManager.ChannelIsPlaying(ChannelType.BGM)) 
            return;
        var speed = PlaybackSpeed;
        Console.WriteLine(speed);
        speed += 0.25f;
        PlbSpdLabel.Content = speed * 100 + "%";
        PlaybackSpeed = speed;
        PlbSpdAdjGrid.Visibility = Visibility.Visible;
        playbackSpeedHideTimer.Stop();
        playbackSpeedHideTimer.Start();
    }
    /// <summary>
    /// 降低播放速度
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DecreasePlaybackSpeed_CanExecute(object? sender, CanExecuteRoutedEventArgs e)
    {
        if (AudioManager.ChannelIsPlaying(ChannelType.BGM)) 
            return;
        var speed = PlaybackSpeed;
        Console.WriteLine(speed);
        speed -= 0.25f;
        if (speed < 1e-6) return; // Interrupt if it's an epsilon or lower.
        PlbSpdLabel.Content = speed * 100 + "%";
        PlaybackSpeed = speed;
        PlbSpdAdjGrid.Visibility = Visibility.Visible;
        playbackSpeedHideTimer.Stop();
        playbackSpeedHideTimer.Start();
    }
    private void PlbHideTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        Dispatcher.Invoke(() => { PlbSpdAdjGrid.Visibility = Visibility.Collapsed; });
        ((Timer)sender!).Stop();
    }

    private void FindCommand_CanExecute(object? sender, CanExecuteRoutedEventArgs e)
    {
        if (FindGrid.Visibility == Visibility.Collapsed)
        {
            FindGrid.Visibility = Visibility.Visible;
            InputText.Focus();
        }
        else
        {
            FindGrid.Visibility = Visibility.Collapsed;
        }
    }

    private void MirrorLRCommand_CanExecute(object? sender, CanExecuteRoutedEventArgs e)
    {
        MirrorLeftRight_MenuItem_Click(sender, null);
    }

    private void MirrorUDCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        MirrorUpDown_MenuItem_Click(sender, null);
    }

    private void Mirror180Command_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        Mirror180_MenuItem_Click(sender, null);
    }

    private void Mirror45Command_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        Mirror45_MenuItem_Click(sender, null);
    }

    private void MirrorCcw45Command_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        MirrorCcw45_MenuItem_Click(sender, null);
    }
}
