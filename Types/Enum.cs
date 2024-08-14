namespace MajdataEdit.Types;
public enum EditorPlayMethod
{
    Classic, DJAuto, Random, Disabled
}
//*PLAY CONTROL

public enum PlayMethod
{
    /// <summary>
    /// 正常回放
    /// </summary>
    Normal,
    /// <summary>
    /// 录制预览
    /// </summary>
    Op,
    /// <summary>
    /// 录制视频
    /// </summary>
    Record
}
public enum EditorComboIndicator
{
    None,

    // List of viable indicators that won't be a static content.
    // ScoreBorder, AchievementMaxDown, ScoreDownDeluxe are static.
    Combo,
    ScoreClassic,
    AchievementClassic,
    AchievementDownClassic,
    AchievementDeluxe = 11,
    AchievementDownDeluxe,
    ScoreDeluxe,

    // Please prefix custom indicator with C
    CScoreDedeluxe = 101,
    CScoreDownDedeluxe,
    MAX
}

public enum EditorControlMethod
{
    Start,
    Stop,
    /// <summary>
    /// 录制预览
    /// </summary>
    OpStart,
    Pause,
    Continue,
    Record
}
public enum ResponseCode
{
    OK,
    Error
}

