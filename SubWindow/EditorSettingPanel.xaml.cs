using MajdataEdit.Types;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using WPFLocalizeExtension.Engine;

namespace MajdataEdit;

/// <summary>
///     BPMtap.xaml 的交互逻辑
/// </summary>
public partial class EditorSettingPanel : Window
{
    private readonly bool dialogMode;
    private readonly string[] langList = new string[3] { "zh-CN", "en-US", "ja" }; // 语言列表
    private bool saveFlag;

    public EditorSettingPanel(bool _dialogMode = false)
    {
        dialogMode = _dialogMode;
        InitializeComponent();

        if (dialogMode) Cancel_Button.IsEnabled = false;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {

        var curLang = MainWindow.EditorSetting!.Language;
        var boxIndex = -1;
        for (var i = 0; i < langList.Length; i++)
            if (curLang == langList[i])
            {
                boxIndex = i;
                break;
            }

        if (boxIndex == -1)
            // 如果没有语言设置 或者语言未知 就自动切换到English
            boxIndex = 1;

        LanguageComboBox.SelectedIndex = boxIndex;

        RenderModeComboBox.SelectedIndex = (int)MainWindow.EditorSetting.RenderMode;

        ViewerCover.Text = MainWindow.EditorSetting.backgroundCover.ToString();
        ViewerSpeed.Text = MainWindow.EditorSetting.NoteSpeed.ToString("F1"); // 转化为形如"7.0", "9.5"这样的速度
        ViewerTouchSpeed.Text = MainWindow.EditorSetting.TouchSpeed.ToString("F1");
        ComboDisplay.SelectedIndex = Array.IndexOf(
            Enum.GetValues(MainWindow.EditorSetting.comboStatusType.GetType()),
            MainWindow.EditorSetting.comboStatusType
        );
        if (ComboDisplay.SelectedIndex < 0)
            ComboDisplay.SelectedIndex = 0;

        PlayMethod.SelectedIndex = Array.IndexOf(
            Enum.GetValues(MainWindow.EditorSetting.editorPlayMethod.GetType()),
            MainWindow.EditorSetting.editorPlayMethod
        );
        if(PlayMethod.SelectedIndex < 0) 
            PlayMethod.SelectedIndex = 0;

        ChartRefreshDelay.Text = MainWindow.EditorSetting.ChartRefreshDelay.ToString();
        AutoUpdate.IsChecked = MainWindow.EditorSetting.AutoCheckUpdate;
        SmoothSlideAnime.IsChecked = MainWindow.EditorSetting.SmoothSlideAnime;
        SyntaxCheckLevel.SelectedIndex = MainWindow.EditorSetting.SyntaxCheckLevel;
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //LanguageComboBox.SelectedIndex
        LocalizeDictionary.Instance.Culture = new CultureInfo(langList[LanguageComboBox.SelectedIndex]);
    }

    private void RenderModeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RenderOptions.ProcessRenderMode =
            RenderModeComboBox.SelectedIndex == 0 ? RenderMode.Default : RenderMode.SoftwareOnly;
    }

    private void ViewerCover_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var offset = float.Parse(ViewerCover.Text);
        offset += e.Delta > 0 ? 0.1f : -0.1f;
        ViewerCover.Text = offset.ToString();
    }

    private void ViewerSpeed_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var offset = float.Parse(ViewerSpeed.Text);
        offset += e.Delta > 0 ? 0.5f : -0.5f;
        ViewerSpeed.Text = offset.ToString();
    }

    private void ViewerTouchSpeed_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var offset = float.Parse(ViewerTouchSpeed.Text);
        offset += e.Delta > 0 ? 0.5f : -0.5f;
        ViewerTouchSpeed.Text = offset.ToString();
    }

    private void Save_Button_Click(object sender, RoutedEventArgs e)
    {
        var window = (MainWindow)Owner;
        MainWindow.EditorSetting!.Language = langList[LanguageComboBox.SelectedIndex];
        MainWindow.EditorSetting!.RenderMode = (RenderType)RenderModeComboBox.SelectedIndex;
        MainWindow.EditorSetting!.backgroundCover = float.Parse(ViewerCover.Text);
        MainWindow.EditorSetting!.NoteSpeed = float.Parse(ViewerSpeed.Text);
        MainWindow.EditorSetting!.TouchSpeed = float.Parse(ViewerTouchSpeed.Text);
        MainWindow.EditorSetting!.ChartRefreshDelay = int.Parse(ChartRefreshDelay.Text);
        MainWindow.EditorSetting!.AutoCheckUpdate = (bool) AutoUpdate.IsChecked!;
        MainWindow.EditorSetting!.SmoothSlideAnime = (bool) SmoothSlideAnime.IsChecked!;
        MainWindow.EditorSetting!.editorPlayMethod = (EditorPlayMethod)PlayMethod.SelectedIndex;
        MainWindow.EditorSetting!.SyntaxCheckLevel = SyntaxCheckLevel.SelectedIndex;
        // MainWindow.editorSetting.isComboEnabled = (bool) ComboDisplay.IsChecked!;
        MainWindow.EditorSetting!.comboStatusType = (EditorComboIndicator)Enum.GetValues(
            MainWindow.EditorSetting!.comboStatusType.GetType()
        ).GetValue(ComboDisplay.SelectedIndex)!;
        window.SaveEditorSetting();

        window.ViewerCover.Content = MainWindow.EditorSetting.backgroundCover.ToString();
        window.ViewerSpeed.Content = MainWindow.EditorSetting.NoteSpeed.ToString("F1"); // 转化为形如"7.0", "9.5"这样的速度
        window.ViewerTouchSpeed.Content = MainWindow.EditorSetting.TouchSpeed.ToString("F1");
        window.ChartRefreshDelay = MainWindow.EditorSetting.ChartRefreshDelay;


        saveFlag = true;
        window.SyntaxCheck();
        Close();
    }

    private void Cancel_Button_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (!saveFlag)
        {
            // 取消或直接关闭窗口
            if (dialogMode)
            {
                // 模态窗口状态下 则阻止关闭
                e.Cancel = true;
                MessageBox.Show(MainWindow.GetLocalizedString("NoEditorSetting"),
                    MainWindow.GetLocalizedString("Error"));
            }
            else
            {
                LocalizeDictionary.Instance.Culture = new CultureInfo(MainWindow.EditorSetting!.Language);
            }
        }
        else
        {
            if (dialogMode) DialogResult = true;
        }
    }
}