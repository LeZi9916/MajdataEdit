namespace MajdataEdit.Types;

class SoundEffectTiming
{
    public readonly bool hasAllPerfect;
    public readonly bool hasClock;
    public readonly double time;
    public bool hasAnswer;
    public bool hasBreak;
    public bool hasBreakSlide;
    public bool hasBreakSlideStart;
    public bool hasHanabi;
    public bool hasJudge;
    public bool hasJudgeBreak;
    public bool hasJudgeBreakSlide;
    public bool hasJudgeEx;
    public bool hasSlide;
    public bool hasTouch;
    public bool hasTouchHold;
    public bool hasTouchHoldEnd;
    public int noteGroupIndex = -1;

    public SoundEffectTiming(double _time, bool _hasAnswer = false, bool _hasJudge = false,
        bool _hasJudgeBreak = false,
        bool _hasBreak = false, bool _hasTouch = false, bool _hasHanabi = false,
        bool _hasJudgeEx = false, bool _hasTouchHold = false, bool _hasSlide = false,
        bool _hasTouchHoldEnd = false, bool _hasAllPerfect = false, bool _hasClock = false,
        bool _hasBreakSlideStart = false, bool _hasBreakSlide = false, bool _hasJudgeBreakSlide = false)
    {
        time = _time;
        hasAnswer = _hasAnswer;
        hasJudge = _hasJudge;
        hasJudgeBreak = _hasJudgeBreak; // 我是笨蛋
        hasBreak = _hasBreak;
        hasTouch = _hasTouch;
        hasHanabi = _hasHanabi;
        hasJudgeEx = _hasJudgeEx;
        hasTouchHold = _hasTouchHold;
        hasSlide = _hasSlide;
        hasTouchHoldEnd = _hasTouchHoldEnd;
        hasAllPerfect = _hasAllPerfect;
        hasClock = _hasClock;
        hasBreakSlideStart = _hasBreakSlideStart;
        hasBreakSlide = _hasBreakSlide;
        hasJudgeBreakSlide = _hasJudgeBreakSlide;
    }
}