using MajdataEdit.Modules.SyntaxModule;
using MajdataEdit.Types;
using MajdataEdit.Utils;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MajdataEdit;
public partial class MainWindow : Window
{
    #region Left
    private async void PlayAndPauseButton_Click(object sender, RoutedEventArgs e)
    {
        SetControlButtonActive(false);
        await TogglePlayAndPause();
        SetControlButtonActive(true);
    }

    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        SetControlButtonActive(false);
        await ToggleStop();
        SetControlButtonActive(true);
    }

    private async void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var i = LevelSelector.SelectedIndex;
        SetRawFumenText(SimaiProcessor.fumens[i]);
        selectedDifficulty = i;
        LevelTextBox.Text = SimaiProcessor.levels[selectedDifficulty];
        SetSavedState(true);
        SimaiProcessor.Serialize(GetRawFumenText());
        await DrawWave();
        SyntaxCheck();
    }

    private void LevelTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SetSavedState(false);
        if (selectedDifficulty == -1) return;
        SimaiProcessor.levels[selectedDifficulty] = LevelTextBox.Text;
    }

    private async void OffsetTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SetSavedState(false);
        try
        {
            SimaiProcessor.first = float.Parse(OffsetTextBox.Text);
            SimaiProcessor.Serialize(GetRawFumenText());
            await DrawWave();
        }
        catch
        {
            SimaiProcessor.first = 0f;
        }
    }

    private void OffsetTextBox_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var offset = float.Parse(OffsetTextBox.Text);
        offset += e.Delta > 0 ? 0.01f : -0.01f;
        OffsetTextBox.Text = offset.ToString();
    }

    private void FollowPlayCheck_Click(object sender, RoutedEventArgs e)
    {
        FumenContent.Focus();
    }

    private async void Op_Button_Click(object sender, RoutedEventArgs e)
    {
        SetControlButtonActive(false);
        await TogglePlayAndStop(PlayMethod.Op);
        SetControlButtonActive(true);
    }

    private void SettingLabel_MouseUp(object sender, MouseButtonEventArgs e)
    {
        // 单击设置的时候也可以进入设置界面
        var esp = new EditorSettingPanel();
        esp.Owner = this;
        esp.ShowDialog();
    }
    #endregion
    #region MENU BARS

    private async void Menu_New_Click(object sender, RoutedEventArgs e)
    {
        if (!isSaved)
            if (!AskSave())
                return;
        var openFileDialog = new OpenFileDialog
        {
            Filter = "track.mp3, track.ogg|track.mp3;track.ogg"
        };
        if ((bool)openFileDialog.ShowDialog()!)
        {
            var fileInfo = new FileInfo(openFileDialog.FileName);
            CreateNewFumen(fileInfo.DirectoryName!);
            await initFromFile(fileInfo.DirectoryName!);
        }
    }

    private async void Menu_Open_Click(object sender, RoutedEventArgs e)
    {
        if (!isSaved)
            if (!AskSave())
                return;
        var openFileDialog = new OpenFileDialog
        {
            Filter = "maidata.txt|maidata.txt"
        };
        if ((bool)openFileDialog.ShowDialog()!)
        {
            var fileInfo = new FileInfo(openFileDialog.FileName);
            await initFromFile(fileInfo.DirectoryName!);
        }
    }

    private async void Menu_Save_Click(object sender, RoutedEventArgs e)
    {
        await SaveFumen(true);
        SystemSounds.Beep.Play();
    }

    private void Menu_SaveAs_Click(object sender, RoutedEventArgs e)
    {
    }

    private async void Menu_ExportRender_Click(object sender, RoutedEventArgs e)
    {
        SetControlButtonActive(false);
        await TogglePlayAndPause(PlayMethod.Record);
        SetControlButtonActive(true);
    }

    private void MirrorLeftRight_MenuItem_Click(object? sender, RoutedEventArgs e)
    {
        var result = Mirror.NoteMirrorHandle(FumenContent.Selection.Text, Mirror.HandleType.LRMirror);
        FumenContent.Selection.Text = result;
    }

    private void MirrorUpDown_MenuItem_Click(object? sender, RoutedEventArgs e)
    {
        var result = Mirror.NoteMirrorHandle(FumenContent.Selection.Text, Mirror.HandleType.UDMirror);
        FumenContent.Selection.Text = result;
    }

    private void Mirror180_MenuItem_Click(object? sender, RoutedEventArgs e)
    {
        var result = Mirror.NoteMirrorHandle(FumenContent.Selection.Text, Mirror.HandleType.HalfRotation);
        FumenContent.Selection.Text = result;
    }

    private void Mirror45_MenuItem_Click(object? sender, RoutedEventArgs e)
    {
        var result = Mirror.NoteMirrorHandle(FumenContent.Selection.Text, Mirror.HandleType.Rotation45);
        FumenContent.Selection.Text = result;
    }

    private void MirrorCcw45_MenuItem_Click(object? sender, RoutedEventArgs e)
    {
        var result = Mirror.NoteMirrorHandle(FumenContent.Selection.Text, Mirror.HandleType.CcwRotation45);
        FumenContent.Selection.Text = result;
    }

    private void BPMtap_MenuItem_Click(object? sender, RoutedEventArgs e)
    {
        var tap = new BPMtap();
        tap.Owner = this;
        tap.Show();
    }

    private void MenuItem_InfomationEdit_Click(object? sender, RoutedEventArgs e)
    {
        var infoWindow = new Infomation();
        SetSavedState(false);
        infoWindow.ShowDialog();
        TheWindow.Title = GetWindowsTitleString(SimaiProcessor.title!);
    }

    private void MenuItem_Majnet_Click(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo() { FileName = "https://majdata.net", UseShellExecute = true });
        //maidata.txtの譜面書式
    }

    private void MenuItem_GitHub_Click(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo() { FileName = "https://github.com/LingFeng-bbben/MajdataView", UseShellExecute = true });
    }

    private void MenuItem_SoundSetting_Click(object? sender, RoutedEventArgs e)
    {
        soundSetting = new SoundSetting
        {
            Owner = this
        };
        soundSetting.ShowDialog();
    }

    private void MuriCheck_Click_1(object? sender, RoutedEventArgs e)
    {
        var muriCheck = new MuriCheck
        {
            Owner = this
        };
        muriCheck.Show();
    }
    private void SyntaxCheckButton_Click(object sender, RoutedEventArgs e)
    {
        ShowErrorWindow();
    }
    void ShowErrorWindow()
    {
        var mcrWindow = new MuriCheckResult
        {
            Owner = this
        };
        var errList = SyntaxChecker.ErrorList;
        errList.ForEach(e =>
        {
            e.positionY--;
            mcrWindow.errorPosition.Add(e);
            var eRow = new ListBoxItem
            {
                Content = e.eMessage,
                Name = "rr" + mcrWindow.CheckResult_Listbox.Items.Count
            };
            eRow.AddHandler(PreviewMouseDoubleClickEvent,
                new MouseButtonEventHandler(mcrWindow.ListBoxItem_PreviewMouseDoubleClick));
            mcrWindow.CheckResult_Listbox.Items.Add(eRow);
        });
        mcrWindow.Show();
    }
    private void SyntaxCheckButton_Click(object sender, MouseButtonEventArgs e)
    {
        ShowErrorWindow();
    }
    private void MenuItem_EditorSetting_Click(object? sender, RoutedEventArgs e)
    {
        var esp = new EditorSettingPanel
        {
            Owner = this
        };
        esp.ShowDialog();
    }

    private void Menu_ResetViewWindow(object? sender, RoutedEventArgs e)
    {
        if (CheckAndStartView()) return;
        InternalSwitchWindow();
    }

    private void MenuFind_Click(object? sender, RoutedEventArgs e)
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

    private async void CheckUpdate_Click(object? sender, RoutedEventArgs e)
    {
        CheckUpdateButton.IsEnabled = false;
        await CheckUpdate();
        CheckUpdateButton.IsEnabled = true;
    }

    private void Menu_AutosaveRecover_Click(object? sender, RoutedEventArgs e)
    {
        var asr = new AutoSaveRecover
        {
            Owner = this
        };
        asr.ShowDialog();
    }

    #endregion
}
