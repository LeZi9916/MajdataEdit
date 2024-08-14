using MajdataEdit.Types;
using System.Media;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using Un4seen.Bass;
using Timer = System.Timers.Timer;

namespace MajdataEdit;
public partial class MainWindow : Window
{
    private void PlayAndPause_CanExecute(object? sender, CanExecuteRoutedEventArgs e) //快捷键
    {
        TogglePlayAndStop();
    }

    private async void StopPlaying_CanExecute(object? sender, CanExecuteRoutedEventArgs e) //快捷键
    {
        PlayAndPauseButton.IsEnabled = false;
        StopButton.IsEnabled = false;
        await TogglePlayAndPause();
        PlayAndPauseButton.IsEnabled = true;
        StopButton.IsEnabled = true;
    }

    private void SaveFile_Command_CanExecute(object? sender, CanExecuteRoutedEventArgs e)
    {
        SaveFumen(true);
        SystemSounds.Beep.Play();
    }

    private void SendToView_CanExecute(object? sender, CanExecuteRoutedEventArgs e)
    {
        TogglePlayAndStop(PlayMethod.Op);
    }

    private void IncreasePlaybackSpeed_CanExecute(object? sender, CanExecuteRoutedEventArgs e)
    {
        if (Bass.BASS_ChannelIsActive(bgmStream) == BASSActive.BASS_ACTIVE_PLAYING) return;
        var speed = GetPlaybackSpeed();
        Console.WriteLine(speed);
        speed += 0.25f;
        PlbSpdLabel.Content = speed * 100 + "%";
        SetPlaybackSpeed(speed);
        PlbSpdAdjGrid.Visibility = Visibility.Visible;
        playbackSpeedHideTimer.Stop();
        playbackSpeedHideTimer.Start();
    }

    private void DecreasePlaybackSpeed_CanExecute(object? sender, CanExecuteRoutedEventArgs e)
    {
        if (Bass.BASS_ChannelIsActive(bgmStream) == BASSActive.BASS_ACTIVE_PLAYING) return;
        var speed = GetPlaybackSpeed();
        Console.WriteLine(speed);
        speed -= 0.25f;
        if (speed < 1e-6) return; // Interrupt if it's an epsilon or lower.
        PlbSpdLabel.Content = speed * 100 + "%";
        SetPlaybackSpeed(speed);
        PlbSpdAdjGrid.Visibility = Visibility.Visible;
        playbackSpeedHideTimer.Stop();
        playbackSpeedHideTimer.Start();
    }

    private readonly Timer playbackSpeedHideTimer = new(1000);

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
