using MajdataEdit.Types;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;
using Path = System.IO.Path;

namespace MajdataEdit.Utils;
public static class AudioManager
{
    #region AUDIO STREAM
    static int allperfectStream = -114514;
    static int answerStream = -114514;

    static int bgmStream = -114514;
    static int breakSlideStartStream = -114514; // break-slide启动音效
    static int breakSlideStream = -114514; // break-slide欢呼声（critical perfect音效）
    static int breakStream = -114514; // 这个才是欢呼声
    static int clockStream = -114514;

    static int fanfareStream = -114514;
    static int hanabiStream = -114514;
    static int holdRiserStream = -114514;

    static int judgeBreakSlideStream = -114514; // break-slide判定音效
    static int judgeBreakStream = -114514; // 这个是break的判定音效 不是欢呼声
    static int judgeExStream = -114514;
    static int judgeStream = -114514;

    static int slideStream = -114514;
    static int touchStream = -114514;
    static int trackStartStream = -114514;

    #endregion
    public static string SFX_PATH => Path.Combine(Environment.CurrentDirectory,"SFX");

    public static void Init()
    {
        answerStream = Bass.BASS_StreamCreateFile(Path.Combine(SFX_PATH, "answer.wav"), 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
        judgeStream = Bass.BASS_StreamCreateFile(Path.Combine(SFX_PATH, "judge.wav"), 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
        judgeBreakStream = Bass.BASS_StreamCreateFile(Path.Combine(SFX_PATH, "judge_break.wav"), 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
        judgeExStream = Bass.BASS_StreamCreateFile(Path.Combine(SFX_PATH, "judge_ex.wav"), 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
        breakStream = Bass.BASS_StreamCreateFile(Path.Combine(SFX_PATH, "break.wav"), 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
        hanabiStream = Bass.BASS_StreamCreateFile(Path.Combine(SFX_PATH, "hanabi.wav"), 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
        holdRiserStream = Bass.BASS_StreamCreateFile(Path.Combine(SFX_PATH, "touchHold_riser.wav"), 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
        trackStartStream = Bass.BASS_StreamCreateFile(Path.Combine(SFX_PATH, "track_start.wav"), 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
        slideStream = Bass.BASS_StreamCreateFile(Path.Combine(SFX_PATH, "slide.wav"), 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
        touchStream = Bass.BASS_StreamCreateFile(Path.Combine(SFX_PATH, "touch.wav"), 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
        allperfectStream = Bass.BASS_StreamCreateFile(Path.Combine(SFX_PATH, "all_perfect.wav"), 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
        fanfareStream = Bass.BASS_StreamCreateFile(Path.Combine(SFX_PATH, "fanfare.wav"), 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
        clockStream = Bass.BASS_StreamCreateFile(Path.Combine(SFX_PATH, "clock.wav"), 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
        breakSlideStartStream = Bass.BASS_StreamCreateFile(Path.Combine(SFX_PATH, "break_slide_start.wav"), 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
        breakSlideStream = Bass.BASS_StreamCreateFile(Path.Combine(SFX_PATH, "break_slide.wav"), 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);
        judgeBreakSlideStream = Bass.BASS_StreamCreateFile(Path.Combine(SFX_PATH, "judge_break_slide.wav"), 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);

    }
    public static BASS_CHANNELINFO? LoadBGM(string path)
    {
        if (!IsInvaildChannel(ChannelType.BGM))
            DisposalChannel(ChannelType.BGM);

        var decodeStream = Bass.BASS_StreamCreateFile(path, 0L, 0L, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_STREAM_PRESCAN);
        bgmStream = BassFx.BASS_FX_TempoCreate(decodeStream, BASSFlag.BASS_FX_FREESOURCE);

        return Bass.BASS_ChannelGetInfo(bgmStream);
    }
    public static void ReadSetting(MajSetting setting)
    {
        Bass.BASS_ChannelSetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
        Bass.BASS_ChannelSetAttribute(trackStartStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
        Bass.BASS_ChannelSetAttribute(allperfectStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
        Bass.BASS_ChannelSetAttribute(fanfareStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
        Bass.BASS_ChannelSetAttribute(clockStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
        Bass.BASS_ChannelSetAttribute(answerStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Answer_Level);
        Bass.BASS_ChannelSetAttribute(judgeStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Judge_Level);
        Bass.BASS_ChannelSetAttribute(judgeBreakStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Break_Level);
        Bass.BASS_ChannelSetAttribute(judgeBreakSlideStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Break_Slide_Level);
        Bass.BASS_ChannelSetAttribute(slideStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Slide_Level);
        Bass.BASS_ChannelSetAttribute(breakSlideStartStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Slide_Level);
        Bass.BASS_ChannelSetAttribute(breakStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Break_Level);
        Bass.BASS_ChannelSetAttribute(breakSlideStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Break_Slide_Level);
        Bass.BASS_ChannelSetAttribute(judgeExStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Ex_Level);
        Bass.BASS_ChannelSetAttribute(touchStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Touch_Level);
        Bass.BASS_ChannelSetAttribute(hanabiStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Hanabi_Level);
        Bass.BASS_ChannelSetAttribute(holdRiserStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Hanabi_Level);
    }
    public static void SaveSetting(MajSetting setting)
    {
        setting.BGM_Level = GetVolume(ChannelType.BGM);
        setting.Answer_Level = GetVolume(ChannelType.Answer);
        setting.Judge_Level = GetVolume(ChannelType.TapJudge);
        setting.Break_Level = GetVolume(ChannelType.BreakJudge);
        setting.Break_Slide_Level = GetVolume(ChannelType.BreakSlideEnd);
        setting.Ex_Level = GetVolume(ChannelType.ExJudge);
        setting.Touch_Level = GetVolume(ChannelType.Touch);
        setting.Slide_Level = GetVolume(ChannelType.Slide);
        setting.Hanabi_Level = GetVolume(ChannelType.Hanabi);
    }

    #region Control
    public static bool Play(in ChannelType type, bool restart) => Bass.BASS_ChannelPlay(GetChannelId(type), restart);
    public static bool Play(in ChannelType type, int pos)
    {
        if (IsInvaildChannel(type))
            return false;
        var stream = GetChannelId(type);
        Bass.BASS_ChannelSetPosition(stream, pos);
        return Bass.BASS_ChannelPlay(stream, false);
    }
    public static bool Stop(in ChannelType type) => Bass.BASS_ChannelStop(GetChannelId(type));
    public static bool Pause(in ChannelType type) => Bass.BASS_ChannelPause(GetChannelId(type));
    #endregion
    public static bool ChannelIsPlaying(in ChannelType type) => GetChannelState(type) == BASSActive.BASS_ACTIVE_PLAYING;
    public static bool ChannelIsStopped(in ChannelType type) => GetChannelState(type) == BASSActive.BASS_ACTIVE_STOPPED;
    public static bool ChannelIsPaused(in ChannelType type) => GetChannelState(type) == BASSActive.BASS_ACTIVE_PAUSED;
    public static BASSActive GetChannelState(in ChannelType type) => Bass.BASS_ChannelIsActive(GetChannelId(type));
    public static bool IsInvaildChannel(in ChannelType type) => GetChannelId(type) == 0;
    public static int GetChannelData(in ChannelType type, ref float[] buffer, int length)
    {
        var stream = GetChannelId(type);
        return Bass.BASS_ChannelGetData(stream, buffer, length);
    }
    public static bool SetPlaybackSpeed(in ChannelType type, in float scale)
    {
        var stream = GetChannelId(type);
        return Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_TEMPO, scale);
    }
    public static bool GetPlaybackSpeed(in ChannelType type, ref float value)
    {
        var stream = GetChannelId(type);
        return Bass.BASS_ChannelGetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_TEMPO, ref value);
    }
    public static float GetPlaybackSpeed(in ChannelType type)
    {
        float value = 0;
        var stream = GetChannelId(type);
        Bass.BASS_ChannelGetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_TEMPO, ref value);
        return value;
    }
    public static void SetPosition(in ChannelType type, int pos)
    {
        if (IsInvaildChannel(type))
            return;
        var stream = GetChannelId(type);
        Bass.BASS_ChannelSetPosition(stream, pos);
    }
    public static void SetSeconds(in ChannelType type, double seconds)
    {
        if (IsInvaildChannel(type))
            return;
        var stream = GetChannelId(type);
        Bass.BASS_ChannelSetPosition(stream, seconds);
    }
    public static BASS_CHANNELINFO? GetChannelInfo(in ChannelType type)
    {
        if (IsInvaildChannel(type))
            return null;
        var stream = GetChannelId(type);
        return Bass.BASS_ChannelGetInfo(stream);
    }
    public static double GetLength(in ChannelType type) => Bass.BASS_ChannelBytes2Seconds(GetChannelId(type), Bass.BASS_ChannelGetLength(GetChannelId(type)));
    public static double GetSeconds(in ChannelType type)
    {
        var stream = GetChannelId(type);
        return Bass.BASS_ChannelBytes2Seconds(stream, Bass.BASS_ChannelGetPosition(stream));
    }
    public static long GetPosition(in ChannelType type)
    {
        var stream = GetChannelId(type);
        return Bass.BASS_ChannelGetPosition(stream);
    }
    /// <summary>
    /// Get bass sound channel id by <paramref name="type"/>
    /// </summary>
    /// <param name="type"></param>
    /// <returns>bass sound channel id,return -1 if invaild</returns>
    public static int GetChannelId(in ChannelType type)
    {
        return type switch
        {
            ChannelType.Answer => answerStream,
            ChannelType.APSFX => allperfectStream,
            ChannelType.BGM => bgmStream,
            ChannelType.BreakSlideStart => breakSlideStartStream,
            ChannelType.BreakSlideEnd => breakSlideStream,
            ChannelType.Break => breakStream,
            ChannelType.Clock => clockStream,
            ChannelType.Fanfare => fanfareStream,
            ChannelType.Hanabi => hanabiStream,
            ChannelType.HoldRiser => holdRiserStream,
            ChannelType.BreakSlideJudge => judgeBreakSlideStream,
            ChannelType.BreakJudge => judgeBreakStream,
            ChannelType.ExJudge => judgeExStream,
            ChannelType.TapJudge => judgeStream,
            ChannelType.Touch => touchStream,
            ChannelType.TrackStart => trackStartStream,
            ChannelType.Slide => slideStream,
            _ => -1
        };
    }
    static void SetChannelId(in ChannelType type, in int value)
    {
        switch (type)
        {
            case ChannelType.Answer:
                answerStream = value;
                break;
            case ChannelType.APSFX:
                allperfectStream = value;
                break;
            case ChannelType.BGM:
                bgmStream = value;
                break;
            case ChannelType.BreakSlideStart:
                breakSlideStartStream = value;
                break;
            case ChannelType.BreakSlideEnd:
                breakSlideStream = value;
                break;
            case ChannelType.Break:
                breakStream = value;
                break;
            case ChannelType.Clock:
                clockStream = value;
                break;
            case ChannelType.Fanfare:
                fanfareStream = value;
                break;
            case ChannelType.Hanabi:
                hanabiStream = value;
                break;
            case ChannelType.HoldRiser:
                holdRiserStream = value;
                break;
            case ChannelType.BreakSlideJudge:
                judgeBreakSlideStream = value;
                break;
            case ChannelType.BreakJudge:
                judgeBreakStream = value;
                break;
            case ChannelType.ExJudge:
                judgeExStream = value;
                break;
            case ChannelType.TapJudge:
                judgeStream = value;
                break;
            case ChannelType.Touch:
                touchStream = value;
                break;
            case ChannelType.TrackStart:
                trackStartStream = value;
                break;
        };
    }
    public static float GetVolume(in ChannelType channel)
    {
        float vol = 0;
        Bass.BASS_ChannelGetAttribute(GetChannelId(channel), BASSAttribute.BASS_ATTRIB_VOL, ref vol);
        return vol;
    }
    public static double GetChannelDB(in ChannelType channel)
    {
        var stream = GetChannelId(channel);
        var ampLevel = GetVolume(channel);
        return Un4seen.Bass.Utils.LevelToDB(Un4seen.Bass.Utils.LowWord(Bass.BASS_ChannelGetLevel(stream)) * ampLevel, 32768) + 40;
    }
    public static void GetVolume(in ChannelType channel, ref float vol) => Bass.BASS_ChannelGetAttribute(GetChannelId(channel), BASSAttribute.BASS_ATTRIB_VOL, ref vol);
    public static void SetVolume(in ChannelType channel, in float vol) => Bass.BASS_ChannelSetAttribute(GetChannelId(channel), BASSAttribute.BASS_ATTRIB_VOL, vol);
    public static void Disposal()
    {
        Bass.BASS_ChannelStop(bgmStream);
        Bass.BASS_StreamFree(bgmStream);
        Bass.BASS_ChannelStop(answerStream);
        Bass.BASS_StreamFree(answerStream);
        Bass.BASS_ChannelStop(breakStream);
        Bass.BASS_StreamFree(breakStream);
        Bass.BASS_ChannelStop(judgeExStream);
        Bass.BASS_StreamFree(judgeExStream);
        Bass.BASS_ChannelStop(hanabiStream);
        Bass.BASS_StreamFree(hanabiStream);
        Bass.BASS_Stop();
        Bass.BASS_Free();
    }
    public static void DisposalChannel(in ChannelType channel)
    {
        var stream = GetChannelId(channel);
        if (IsInvaildChannel(channel))
            return;
        Bass.BASS_ChannelStop(stream);
        Bass.BASS_StreamFree(stream);

        SetChannelId(channel, 0);
    }
}
