namespace MajdataEdit.Types;

//this setting is global
public class EditorSetting
{
    public bool AutoCheckUpdate = true;
    public float backgroundCover = 0.6f;
    public int ChartRefreshDelay = 1000;
    public EditorComboIndicator comboStatusType = 0;
    public EditorPlayMethod editorPlayMethod;
    public string DecreasePlaybackSpeedKey = "Ctrl+o";
    public float Default_Answer_Level = 0.7f;
    public float Default_BGM_Level = 0.7f;
    public float Default_Break_Level = 0.7f;
    public float Default_Break_Slide_Level = 0.7f;
    public float Default_Ex_Level = 0.7f;
    public float Default_Hanabi_Level = 0.7f;
    public float Default_Judge_Level = 0.7f;
    public float Default_Slide_Level = 0.7f;
    public float Default_Touch_Level = 0.7f;
    public float DefaultSlideAccuracy = 0.2f;
    public float FontSize = 12;
    public string IncreasePlaybackSpeedKey = "Ctrl+p";
    public string Language = "en-US";
    public string Mirror180Key = "Ctrl+l";
    public string Mirror45Key = "Ctrl+OemSemicolon";
    public string MirrorCcw45Key = "Ctrl+OemQuotes";
    public string MirrorLeftRightKey = "Ctrl+j";
    public string MirrorUpDownKey = "Ctrl+k";
    public string PlayPauseKey = "Ctrl+Shift+c";
    public float playSpeed = 7.5f;
    public string PlayStopKey = "Ctrl+Shift+x";
    public int RenderMode = 0; //0=硬件渲染(默认)，1=软件渲染
    public int SyntaxCheckLevel = 1; //0=禁用，1=警告(默认)，2=启用
    public string SaveKey = "Ctrl+s";
    public string SendViewerKey = "Ctrl+Shift+z";
    public float touchSpeed = 7.5f;
    public bool SmoothSlideAnime = false;
}