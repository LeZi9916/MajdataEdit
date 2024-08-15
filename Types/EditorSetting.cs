namespace MajdataEdit.Types;

//this setting is global
public class EditorSetting
{
    public bool AutoCheckUpdate { get; set; } = true;
    public float backgroundCover { get; set; } = 0.6f;
    public int ChartRefreshDelay { get; set; } = 1000;
    public EditorComboIndicator comboStatusType { get; set; } = EditorComboIndicator.None;
    public EditorPlayMethod editorPlayMethod { get; set; } = EditorPlayMethod.DJAuto;
    public string DecreasePlaybackSpeedKey { get; set; } = "Ctrl+o";
    public float Default_Answer_Level { get; set; } = 0.7f;
    public float Default_BGM_Level { get; set; } = 0.7f;
    public float Default_Break_Level { get; set; } = 0.7f;
    public float Default_Break_Slide_Level { get; set; } = 0.7f;
    public float Default_Ex_Level { get; set; } = 0.7f;
    public float Default_Hanabi_Level { get; set; } = 0.7f;
    public float Default_Judge_Level { get; set; } = 0.7f;
    public float Default_Slide_Level { get; set; } = 0.7f;
    public float Default_Touch_Level { get; set; } = 0.7f;
    public float DefaultSlideAccuracy { get; set; } = 0.2f;
    public float FontSize { get; set; } = 12;
    public string IncreasePlaybackSpeedKey { get; set; } = "Ctrl+p";
    public string Language { get; set; } = "en-US";
    public string Mirror180Key { get; set; } = "Ctrl+l";
    public string Mirror45Key { get; set; } = "Ctrl+OemSemicolon";
    public string MirrorCcw45Key { get; set; } = "Ctrl+OemQuotes";
    public string MirrorLeftRightKey { get; set; } = "Ctrl+j";
    public string MirrorUpDownKey { get; set; } = "Ctrl+k";
    public string PlayPauseKey { get; set; } = "Ctrl+Shift+c";
    public float NoteSpeed { get; set; } = 7.5f;
    public string PlayStopKey { get; set; } = "Ctrl+Shift+x";
    public RenderType RenderMode { get; set; } = RenderType.HW; //0=硬件渲染(默认)，1=软件渲染
    public int SyntaxCheckLevel { get; set; } = 1; //0=禁用，1=警告(默认)，2=启用
    public string SaveKey { get; set; } = "Ctrl+s";
    public string SendViewerKey { get; set; } = "Ctrl+Shift+z";
    public float TouchSpeed { get; set; } = 7.5f;
    public bool SmoothSlideAnime { get; set; } = false;

    
}