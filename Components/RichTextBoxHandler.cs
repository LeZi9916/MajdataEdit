using MajdataEdit.Types;
using MajdataEdit.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace MajdataEdit;
public partial class MainWindow : Window
{
    private async void FumenContent_SelectionChanged(object sender, RoutedEventArgs e)
    {
        NoteNowText.Content = "" + (
            new TextRange(FumenContent.Document.ContentStart, FumenContent.CaretPosition).Text.Replace("\r", "")
                .Count(o => o == '\n') + 1) + " 行";
        if (AudioManager.ChannelIsPlaying(ChannelType.BGM) && (bool)FollowPlayCheck.IsChecked!)
            return;
        //TODO:这个应该换成用fumen text position来在已经serialized的timinglist里面找。。 然后直接去掉这个double的返回和position的入参。。。
        var time = SimaiProcessor.Serialize(GetRawFumenText(), GetRawFumenPosition());

        //按住Ctrl，同时按下鼠标左键/上下左右方向键时，才改变进度，其他包含Ctrl的组合键不影响进度。
        if (Keyboard.Modifiers == ModifierKeys.Control && (
                Mouse.LeftButton == MouseButtonState.Pressed ||
                Keyboard.IsKeyDown(Key.Left) ||
                Keyboard.IsKeyDown(Key.Right) ||
                Keyboard.IsKeyDown(Key.Up) ||
                Keyboard.IsKeyDown(Key.Down)
            ))
        {
            if (AudioManager.ChannelIsPlaying(ChannelType.BGM))
                await TogglePause();
            await SetBgmPosition(time);
        }

        //Console.WriteLine("SelectionChanged");
        SimaiProcessor.ClearNoteListPlayedState();
        ghostCusorPositionTime = (float)time;
        if (!isPlaying) await DrawWave();
    }

    private void FumenContent_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (GetRawFumenText() == "" || isLoading)
            return;
        SetSavedState(false);
    }

    private void FumenContent_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // 按下Insert键，同时未按下任何组合键，切换覆盖模式
        if (e.Key == Key.Insert && Keyboard.Modifiers == ModifierKeys.None)
        {
            SwitchFumenOverwriteMode();
            e.Handled = true;
        }
    }
}
