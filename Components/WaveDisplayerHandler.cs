using System.Windows;
using System.Windows.Input;

namespace MajdataEdit;
public partial class MainWindow : Window
{
    private async void WaveViewZoomIn_Click(object sender, RoutedEventArgs e)
    {
        if (deltatime > 1)
            deltatime -= 1;
        await DrawWave();
        FumenContent.Focus();
    }

    private async void WaveViewZoomOut_Click(object sender, RoutedEventArgs e)
    {
        if (deltatime < 10)
            deltatime += 1;
        await DrawWave();
        FumenContent.Focus();
    }

    private async void MusicWave_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        await ScrollWave(-e.Delta);
    }

    private void MusicWave_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        lastMousePointX = e.GetPosition(this).X;
    }

    private async void MusicWave_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            var delta = e.GetPosition(this).X - lastMousePointX;
            lastMousePointX = e.GetPosition(this).X;
            await ScrollWave(-delta);
        }

        lastMousePointX = e.GetPosition(this).X;
    }

    private async void MusicWave_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        InitWave();
        await DrawWave();
    }
}
