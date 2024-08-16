using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using MajdataEdit.Utils;
using MajdataEdit.Types;
using Timer = System.Timers.Timer;

namespace MajdataEdit;

public partial class MainWindow
{
    readonly Timer waveStopMonitorTimer = new(33);

    double extraTime4AllPerfect; // 需要在播放完后等待All Perfect特效的秒数
    bool isPlan2Stop;            // 已准备停止 当all perfect无法在播放完BGM前结束时需要此功能
    bool isPlaying;              // 为了解决播放到结束时自动停止
    double lastPlayTiming = 0;        // 上次播放的Timing

    private List<SoundEffectTiming>? waitToBePlayed { get; set; }
    //private Stopwatch sw = new Stopwatch();

    // This update "middle" frequently to monitor if the wave has to be stopped
    private void WaveStopMonitorTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        WaveStopMonitorUpdate();
    }

    [DllImport("winmm")]
    private static extern void timeBeginPeriod(int t);

    [DllImport("winmm")]
    private static extern void timeEndPeriod(int t);

    private void StartSELoop()
    {
        var thread = new Thread(() =>
        {
            timeBeginPeriod(1);
            var lasttime = AudioManager.GetSeconds(ChannelType.BGM);
            while (!ViewController.IsIdle)
            {
                //sw.Reset();
                //sw.Start();
                if(ViewController.IsPlaying)
                    SoundEffectUpdate();
                Thread.Sleep(1);
                //sw.Stop();
                //if(sw.Elapsed.TotalMilliseconds>1.5)
                //    Console.WriteLine(sw.Elapsed);
            }

            timeEndPeriod(1);
        })
        {
            Priority = ThreadPriority.Highest
        };
        thread.Start();
    }

    private void SoundEffectUpdate()
    {
        try
        {
            var currentTime = AudioManager.GetSeconds(ChannelType.BGM);
            //var waitToBePlayed = SimaiProcess.notelist.FindAll(o => o.havePlayed == false && o.time > currentTime);
            if (waitToBePlayed!.Count < 1) return;
            var nearestTime = waitToBePlayed[0].time;
            //Console.WriteLine(nearestTime - currentTime);
            if (nearestTime - currentTime <= 0.0545) //dont touch this!!!!! this related to delay
            {
                var se = waitToBePlayed[0];
                waitToBePlayed.RemoveAt(0);

                if (se.hasAnswer) AudioManager.Play(ChannelType.Answer,true);
                if (se.hasJudge) AudioManager.Play(ChannelType.TapJudge, true);
                if (se.hasJudgeBreak) AudioManager.Play(ChannelType.BreakJudge, true);
                if (se.hasJudgeEx) AudioManager.Play(ChannelType.ExJudge, true);
                if (se.hasBreak) AudioManager.Play(ChannelType.Break, true);
                if (se.hasTouch) AudioManager.Play(ChannelType.Touch, true);
                if (se.hasHanabi) //may cause delay
                    AudioManager.Play(ChannelType.Hanabi, true);
                if (se.hasTouchHold) AudioManager.Play(ChannelType.HoldRiser, true);
                if (se.hasTouchHoldEnd) AudioManager.Stop(ChannelType.HoldRiser);
                if (se.hasSlide) AudioManager.Play(ChannelType.Slide, true);
                if (se.hasBreakSlideStart) AudioManager.Play(ChannelType.BreakSlideStart, true);
                if (se.hasBreakSlide) AudioManager.Play(ChannelType.BreakSlideEnd, true);
                if (se.hasJudgeBreakSlide) AudioManager.Play(ChannelType.BreakSlideJudge, true);
                if (se.hasAllPerfect)
                {
                    AudioManager.Play(ChannelType.APSFX, true);
                    AudioManager.Play(ChannelType.Fanfare, true);
                }

                if (se.hasClock) AudioManager.Play(ChannelType.Clock, true);
                //
                Dispatcher.Invoke(() =>
                {
                    if ((bool)FollowPlayCheck.IsChecked!)
                    {
                        ghostCusorPositionTime = (float)nearestTime;
                        SeekTextFromIndex(se.noteGroupIndex);
                    }
                });
            }
        }
        catch
        {
        }
    }

    private double GetAllPerfectStartTime()
    {
        // 获取All Perfect理论上播放的时间点 也就是最后一个被完成的note
        double latestNoteFinishTime = -1;
        double baseTime, noteTime;
        foreach (var noteGroup in SimaiProcessor.notelist)
        {
            baseTime = noteGroup.time;
            foreach (var note in noteGroup.getNotes())
            {
                if (note.noteType == SimaiNoteType.Tap || note.noteType == SimaiNoteType.Touch)
                    noteTime = baseTime;
                else if (note.noteType == SimaiNoteType.Hold || note.noteType == SimaiNoteType.TouchHold)
                    noteTime = baseTime + note.holdTime;
                else if (note.noteType == SimaiNoteType.Slide)
                    noteTime = note.slideStartTime + note.slideTime;
                else
                    noteTime = -1;
                if (noteTime > latestNoteFinishTime) latestNoteFinishTime = noteTime;
            }
        }

        return latestNoteFinishTime;
    }

    private async Task GenerateSoundEffectList(double startTime, bool isOpIncluded)
    {
        await Task.Run(() =>
        {
            waitToBePlayed = new List<SoundEffectTiming>();
            if (isOpIncluded)
            {
                var cmds = SimaiProcessor.other_commands!.Split('\n');
                foreach (var cmdl in cmds)
                    if (cmdl.Length > 12 && cmdl.Substring(1, 11) == "clock_count")
                        try
                        {
                            var clock_cnt = int.Parse(cmdl.Substring(13));
                            var clock_int = 60.0d / SimaiProcessor.notelist[0].currentBpm;
                            for (var i = 0; i < clock_cnt; i++)
                                waitToBePlayed.Add(new SoundEffectTiming(i * clock_int, _hasClock: true));
                        }
                        catch
                        {
                        }
            }

            for (var i = 0; i < SimaiProcessor.notelist.Count; i++)
            {
                var noteGroup = SimaiProcessor.notelist[i];
                if (noteGroup.time < startTime) continue;

                SoundEffectTiming stobj;

                // 如果目前为止已经有一个SE了 那么就直接使用这个SE
                var combIndex = waitToBePlayed.FindIndex(o => Math.Abs(o.time - noteGroup.time) < 0.001f);
                if (combIndex != -1)
                    stobj = waitToBePlayed[combIndex];
                else
                    stobj = new SoundEffectTiming(noteGroup.time);

                stobj.noteGroupIndex = i;

                var notes = noteGroup.getNotes();
                foreach (var note in notes)
                    switch (note.noteType)
                    {
                        case SimaiNoteType.Tap:
                            {
                                stobj.hasAnswer = true;
                                if (note.isBreak)
                                {
                                    // 如果是Break 则有Break判定音和Break欢呼音（2600）
                                    stobj.hasBreak = true;
                                    stobj.hasJudgeBreak = true;
                                }

                                if (note.isEx)
                                    // 如果是Ex 则有Ex判定音
                                    stobj.hasJudgeEx = true;
                                if (!note.isBreak && !note.isEx)
                                    // 如果二者皆没有 则是普通note 播放普通判定音
                                    stobj.hasJudge = true;
                                break;
                            }
                        case SimaiNoteType.Hold:
                            {
                                stobj.hasAnswer = true;
                                // 类似于Tap 判断Break和Ex的音效 二者皆无则为普通
                                if (note.isBreak)
                                {
                                    stobj.hasBreak = true;
                                    stobj.hasJudgeBreak = true;
                                }

                                if (note.isEx) stobj.hasJudgeEx = true;
                                if (!note.isBreak && !note.isEx) stobj.hasJudge = true;

                                // 计算Hold尾部的音效
                                if (!(note.holdTime <= 0.00f))
                                {
                                    // 如果是短hold（六角tap），则忽略尾部音效。否则，才会计算尾部音效
                                    var targetTime = noteGroup.time + note.holdTime;
                                    var nearIndex = waitToBePlayed.FindIndex(o => Math.Abs(o.time - targetTime) < 0.001f);
                                    if (nearIndex != -1)
                                    {
                                        waitToBePlayed[nearIndex].hasAnswer = true;
                                        if (!note.isBreak && !note.isEx) waitToBePlayed[nearIndex].hasJudge = true;
                                    }
                                    else
                                    {
                                        // 只有最普通的Hold才有结尾的判定音 Break和Ex型则没有（Break没有为推定）
                                        var holdRelease = new SoundEffectTiming(targetTime, true, !note.isBreak && !note.isEx);
                                        waitToBePlayed.Add(holdRelease);
                                    }
                                }

                                break;
                            }
                        case SimaiNoteType.Slide:
                            {
                                if (!note.isSlideNoHead)
                                {
                                    // 当Slide不是无头星星的时候 才有answer音和判定音
                                    stobj.hasAnswer = true;
                                    if (note.isBreak)
                                    {
                                        stobj.hasBreak = true;
                                        stobj.hasJudgeBreak = true;
                                    }

                                    if (note.isEx) stobj.hasJudgeEx = true;
                                    if (!note.isBreak && !note.isEx) stobj.hasJudge = true;
                                }

                                // Slide启动音效
                                var targetTime = note.slideStartTime;
                                var nearIndex = waitToBePlayed.FindIndex(o => Math.Abs(o.time - targetTime) < 0.001f);
                                if (nearIndex != -1)
                                {
                                    if (note.isSlideBreak)
                                        // 如果是break slide的话 使用break slide的启动音效
                                        waitToBePlayed[nearIndex].hasBreakSlideStart = true;
                                    else
                                        // 否则使用普通slide的启动音效
                                        waitToBePlayed[nearIndex].hasSlide = true;
                                }
                                else
                                {
                                    SoundEffectTiming slide;
                                    if (note.isSlideBreak)
                                        slide = new SoundEffectTiming(targetTime, _hasBreakSlideStart: true);
                                    else
                                        slide = new SoundEffectTiming(targetTime, _hasSlide: true);
                                    waitToBePlayed.Add(slide);
                                }

                                // Slide尾巴 如果是Break Slide的话 就要添加一个Break音效
                                if (note.isSlideBreak)
                                {
                                    targetTime = note.slideStartTime + note.slideTime;
                                    nearIndex = waitToBePlayed.FindIndex(o => Math.Abs(o.time - targetTime) < 0.001f);
                                    if (nearIndex != -1)
                                    {
                                        waitToBePlayed[nearIndex].hasBreakSlide = true;
                                        waitToBePlayed[nearIndex].hasJudgeBreakSlide = true;
                                    }
                                    else
                                    {
                                        var slide = new SoundEffectTiming(targetTime, _hasBreakSlide: true,
                                            _hasJudgeBreakSlide: true);
                                        waitToBePlayed.Add(slide);
                                    }
                                }

                                break;
                            }
                        case SimaiNoteType.Touch:
                            {
                                stobj.hasAnswer = true;
                                stobj.hasTouch = true;
                                if (note.isHanabi) stobj.hasHanabi = true;
                                break;
                            }
                        case SimaiNoteType.TouchHold:
                            {
                                stobj.hasAnswer = true;
                                stobj.hasTouch = true;
                                stobj.hasTouchHold = true;
                                // 计算TouchHold结尾
                                var targetTime = noteGroup.time + note.holdTime;
                                var nearIndex = waitToBePlayed.FindIndex(o => Math.Abs(o.time - targetTime) < 0.001f);
                                if (nearIndex != -1)
                                {
                                    if (note.isHanabi) waitToBePlayed[nearIndex].hasHanabi = true;
                                    waitToBePlayed[nearIndex].hasAnswer = true;
                                    waitToBePlayed[nearIndex].hasTouchHoldEnd = true;
                                }
                                else
                                {
                                    var tHoldRelease = new SoundEffectTiming(targetTime, true, _hasHanabi: note.isHanabi,
                                        _hasTouchHoldEnd: true);
                                    waitToBePlayed.Add(tHoldRelease);
                                }

                                break;
                            }
                    }

                if (combIndex != -1)
                    waitToBePlayed[combIndex] = stobj;
                else
                    waitToBePlayed.Add(stobj);
            }

            if (isOpIncluded) waitToBePlayed.Add(new SoundEffectTiming(GetAllPerfectStartTime(), _hasAllPerfect: true));
            waitToBePlayed.Sort((o1, o2) => o1.time < o2.time ? -1 : 1);

            var apTime = GetAllPerfectStartTime();
            if (songLength < apTime + 4.0)
                // 如果BGM的时长不足以播放完AP特效 这里假设AP特效持续4秒
                extraTime4AllPerfect = apTime + 4.0 - songLength; // 预留给AP的额外时间（播放结束后）
            else
                // 如果足够播完 那么就等到BGM结束再停止
                extraTime4AllPerfect = -1;

            //Console.WriteLine(JsonConvert.SerializeObject(waitToBePlayed));
        });
    }

    /// <summary>
    /// 录制模式预生成wav波形音频文件
    /// </summary>
    /// <param name="delaySeconds"></param>
    /// <exception cref="Exception"></exception>
    private void RenderSoundEffect(double delaySeconds)
    {
        //TODO: 改为异步并增加提示窗口
        var path = Environment.CurrentDirectory + "/SFX";
        var tempPath = GetViewerWorkingDirectory();
        string converterPath;

        var pathEnv = new List<string>
        {
            tempPath
        };
        pathEnv.AddRange(Environment.GetEnvironmentVariable("PATH")!.Split(Path.PathSeparator));
        converterPath = pathEnv.FirstOrDefault(scanPath =>
        {
            return File.Exists(scanPath + "/ffmpeg.exe");
        })!;

        var throwErrorOnMismatch = converterPath.Length == 0;

        //默认参数：16bit
        string getBasePath(string rawPath) { return rawPath.Split('/').Last(); }

        var useOgg = File.Exists(MaidataDir + "/track.ogg");

        var bgmBank = new SoundBank(MaidataDir + "/track" + (useOgg ? ".ogg" : ".mp3"));

        var comparableBanks = new Dictionary<string, SoundBank>();

        var answerBank = new SoundBank(path + "/answer.wav");
        var judgeBank = new SoundBank(path + "/judge.wav");
        var judgeBreakBank = new SoundBank(path + "/judge_break.wav");
        var judgeExBank = new SoundBank(path + "/judge_ex.wav");
        var breakBank = new SoundBank(path + "/break.wav");
        var hanabiBank = new SoundBank(path + "/hanabi.wav");
        var holdRiserBank = new SoundBank(path + "/touchHold_riser.wav");
        var trackStartBank = new SoundBank(path + "/track_start.wav");
        var slideBank = new SoundBank(path + "/slide.wav");
        var touchBank = new SoundBank(path + "/touch.wav");
        var apBank = new SoundBank(path + "/all_perfect.wav");
        var fanfareBank = new SoundBank(path + "/fanfare.wav");
        var clockBank = new SoundBank(path + "/clock.wav");
        var breakSlideStartBank = new SoundBank(path + "/break_slide_start.wav");
        var breakSlideBank = new SoundBank(path + "/break_slide.wav");
        var judgeBreakSlideBank = new SoundBank(path + "/judge_break_slide.wav");

        comparableBanks["Answer"] = answerBank;
        comparableBanks["Judge"] = judgeBank;
        comparableBanks["Judge Break"] = judgeBreakBank;
        comparableBanks["Judge EX"] = judgeExBank;
        comparableBanks["Break"] = breakBank;
        comparableBanks["Hanabi"] = hanabiBank;
        comparableBanks["Hold Riser"] = holdRiserBank;
        comparableBanks["Track Start"] = trackStartBank;
        comparableBanks["Slide"] = slideBank;
        comparableBanks["Touch"] = touchBank;
        comparableBanks["All Perfect"] = apBank;
        comparableBanks["Fanfare"] = fanfareBank;
        comparableBanks["Clock"] = clockBank;
        comparableBanks["Break Slide Start"] = breakSlideStartBank;
        comparableBanks["Break Slide"] = breakSlideBank;
        comparableBanks["Judge Break Slide"] = judgeBreakSlideBank;

        foreach (var compPair in comparableBanks)
        {
            // Skip non existent file.
            if (compPair.Value.Frequency < 0)
                continue;

            if (bgmBank.FrequencyCheck(compPair.Value))
                continue;

            if (throwErrorOnMismatch)
                throw new Exception(
                    string.Format("BGM and {0} do not have same sample rate. Convert the {0} from {1}Hz into {2}Hz!",
                        compPair.Key, compPair.Value.Frequency, bgmBank.Frequency)
                );

            Console.WriteLine("Convert sample of {0} ({1}/{2})...", compPair.Key, compPair.Value.Info!.length,
                compPair.Value.Frequency);
            compPair.Value.Reassign(converterPath, tempPath, "t_" + getBasePath(compPair.Value.FilePath),
                bgmBank.Frequency);
        }

        var freq = bgmBank.Frequency;

        //读取原始采样数据
        var sampleCount = (long)((songLength + 5f) * freq * 2);
        bgmBank.RawSize = sampleCount;
        Console.WriteLine(sampleCount);
        bgmBank.InitializeRawSample();

        foreach (var compPair in comparableBanks)
        {
            // Skip non existent file.
            if (compPair.Value.Frequency < 0)
                continue;

            if (!bgmBank.FrequencyCheck(compPair.Value))
                continue;

            Console.WriteLine("Init sample for {0}...", compPair.Key);
            compPair.Value.InitializeRawSample();
        }

        var trackOps = new List<SoundDataRange>();
        var typeSamples = new Dictionary<SoundDataType, short[]>();
        foreach (SoundDataType sType in Enum.GetValues(SoundDataType.None.GetType()))
        {
            if (sType == 0) continue;
            typeSamples[sType] = new short[sampleCount];
        }

        SoundBank? getSampleFromType(SoundDataType type)
        {
            return type switch
            {
                SoundDataType.Answer => answerBank,
                SoundDataType.Judge => judgeBank,
                SoundDataType.JudgeBreak => judgeBreakBank,
                SoundDataType.JudgeEX => judgeExBank,
                SoundDataType.Break => breakBank,
                SoundDataType.Hanabi => hanabiBank,
                SoundDataType.TouchHold => holdRiserBank,
                SoundDataType.Slide => slideBank,
                SoundDataType.Touch => touchBank,
                SoundDataType.AllPerfect => apBank,
                SoundDataType.FullComboFanfare => fanfareBank,
                SoundDataType.Clock => clockBank,
                SoundDataType.BreakSlideStart => breakSlideStartBank,
                SoundDataType.BreakSlide => breakSlideBank,
                SoundDataType.JudgeBreakSlide => judgeBreakSlideBank,
                _ => null,
            };
        }

        void sampleWrite(int time, SoundDataType type)
        {
            var sample = getSampleFromType(type);
            if (sample == null) return;
            if (sample.Raw == null) return;
            if (sample.Frequency <= 0) return;
            for (var t = 0; t < sample.RawSize && time + t < typeSamples[type].Length; t++)
                typeSamples[type][time + t] = sample.Raw[t];
        }

        void sampleWipe(int timeFrom, int timeTo, SoundDataType type)
        {
            for (var t = timeFrom; t < timeTo && t < typeSamples[type].Length; t++)
                typeSamples[type][t] = 0;
        }

        //生成每个音效的track
        foreach (var soundTiming in waitToBePlayed!)
        {
            var startIndex = (int)(soundTiming.time * freq) * 2; //乘2因为有两个channel
            if (soundTiming.hasAnswer) sampleWrite(startIndex, SoundDataType.Answer);
            if (soundTiming.hasJudge) sampleWrite(startIndex, SoundDataType.Judge);
            if (soundTiming.hasJudgeBreak) sampleWrite(startIndex, SoundDataType.JudgeBreak);
            if (soundTiming.hasJudgeEx) sampleWrite(startIndex, SoundDataType.JudgeEX);
            if (soundTiming.hasBreak)
                // Reach for the Stars.ogg
                sampleWrite(startIndex, SoundDataType.Break);
            if (soundTiming.hasHanabi) sampleWrite(startIndex, SoundDataType.Hanabi);
            if (soundTiming.hasTouchHold)
            {
                // no need to "CutNow" as HoldEnd did the work.
                sampleWrite(startIndex, SoundDataType.TouchHold);
                trackOps.Add(new SoundDataRange(SoundDataType.TouchHold, startIndex, holdRiserBank.RawSize));
            }

            if (soundTiming.hasTouchHoldEnd)
            {
                //不覆盖整个track，只覆盖可能有的部分
                var lastTouchHoldOp = trackOps.FindLast(trackOp => trackOp.Type == SoundDataType.TouchHold);
                sampleWipe(startIndex, (int)lastTouchHoldOp.To, SoundDataType.TouchHold);
                continue;
            }

            if (soundTiming.hasSlide) sampleWrite(startIndex, SoundDataType.Slide);
            if (soundTiming.hasTouch) sampleWrite(startIndex, SoundDataType.Touch);
            if (soundTiming.hasBreakSlideStart) sampleWrite(startIndex, SoundDataType.BreakSlideStart);
            if (soundTiming.hasBreakSlide) sampleWrite(startIndex, SoundDataType.BreakSlide);
            if (soundTiming.hasJudgeBreakSlide) sampleWrite(startIndex, SoundDataType.JudgeBreakSlide);
            if (soundTiming.hasAllPerfect)
            {
                sampleWrite(startIndex, SoundDataType.AllPerfect);
                sampleWrite(startIndex, SoundDataType.FullComboFanfare);
            }

            if (soundTiming.hasClock) sampleWrite(startIndex, SoundDataType.Clock);
        }

        //获取原来实时播放时候的音量

        float bgmVol = AudioManager.GetVolume(ChannelType.BGM),
            answerVol = AudioManager.GetVolume(ChannelType.Answer),
            judgeVol = AudioManager.GetVolume(ChannelType.TapJudge),
            judgeExVol = AudioManager.GetVolume(ChannelType.ExJudge),
            hanabiVol = AudioManager.GetVolume(ChannelType.Hanabi),
            touchVol = AudioManager.GetVolume(ChannelType.Touch),
            slideVol = AudioManager.GetVolume(ChannelType.Slide),
            breakVol = AudioManager.GetVolume(ChannelType.Break),
            breakSlideVol = AudioManager.GetVolume(ChannelType.BreakSlideEnd);

        var filedata = new List<byte>();
        var delayEmpty = new short[(int)(delaySeconds * freq * 2)];
        var filehead = CreateWaveFileHeader(bgmBank.Raw!.Length * 2 + delayEmpty.Length * 2, 2, freq, 16).ToList();

        //if (trackStartRAW.Length > delayEmpty.Length)
        //    throw new Exception("track_start音效过长,请勿大于5秒");

        for (var i = 0; i < delayEmpty.Length; i++)
        {
            if (i < trackStartBank.Raw!.Length)
                delayEmpty[i] = trackStartBank.Raw[i];
            filehead.AddRange(BitConverter.GetBytes(delayEmpty[i]));
        }

        for (var i = 0; i < sampleCount; i++)
        {
            // Apply BGM Data
            var sampleValue = bgmBank.Raw[i] * bgmVol;

            foreach (var sampleTuple in typeSamples)
            {
                var type = sampleTuple.Key;
                var track = sampleTuple.Value;

                switch (type)
                {
                    case SoundDataType.Answer:
                        sampleValue += track[i] * answerVol;
                        break;
                    case SoundDataType.Judge:
                        sampleValue += track[i] * judgeVol;
                        break;
                    case SoundDataType.JudgeBreak:
                        sampleValue += track[i] * breakVol;
                        break;
                    case SoundDataType.JudgeEX:
                        sampleValue += track[i] * judgeExVol;
                        break;
                    case SoundDataType.Break:
                        sampleValue += track[i] * breakVol * 0.75f;
                        break;
                    case SoundDataType.BreakSlide:
                    case SoundDataType.JudgeBreakSlide:
                        sampleValue += track[i] * breakSlideVol;
                        break;
                    case SoundDataType.Hanabi:
                    case SoundDataType.TouchHold:
                        sampleValue += track[i] * hanabiVol;
                        break;
                    case SoundDataType.Slide:
                    case SoundDataType.BreakSlideStart:
                        sampleValue += track[i] * slideVol;
                        break;
                    case SoundDataType.Touch:
                        sampleValue += track[i] * touchVol;
                        break;
                    case SoundDataType.AllPerfect:
                    case SoundDataType.FullComboFanfare:
                    case SoundDataType.Clock:
                        sampleValue += track[i] * bgmVol;
                        break;
                }
            }

            var value = (long)sampleValue;
            if (value > short.MaxValue)
                value = short.MaxValue;
            if (value < short.MinValue)
                value = short.MinValue;
            filedata.AddRange(BitConverter.GetBytes((short)value));
        }

        filehead.AddRange(filedata);
        File.WriteAllBytes(MaidataDir + "/out.wav", filehead.ToArray());

        typeSamples.Clear();
        bgmBank.Free();
        comparableBanks.Values.ToList().ForEach(otherBank =>
        {
            if (otherBank.Temp) File.Delete(otherBank.FilePath);
            otherBank.Free();
        });
    }

    /// <summary>
    ///     创建WAV音频文件头信息,爱来自cnblogs:https://www.cnblogs.com/CUIT-DX037/p/14070754.html
    /// </summary>
    /// <param name="data_Len">音频数据长度</param>
    /// <param name="data_SoundCH">音频声道数</param>
    /// <param name="data_Sample">采样率，常见有：11025、22050、44100等</param>
    /// <param name="data_SamplingBits">采样位数，常见有：4、8、12、16、24、32</param>
    /// <returns></returns>
    private static byte[] CreateWaveFileHeader(int data_Len, int data_SoundCH, int data_Sample, int data_SamplingBits)
    {
        // WAV音频文件头信息
        var WAV_HeaderInfo = new List<byte>(); // 长度应该是44个字节
        WAV_HeaderInfo.AddRange(
            Encoding.ASCII
                .GetBytes("RIFF")); // 4个字节：固定格式，“RIFF”对应的ASCII码，表明这个文件是有效的 "资源互换文件格式（Resources lnterchange File Format）"
        WAV_HeaderInfo.AddRange(BitConverter.GetBytes(data_Len + 44 - 8)); // 4个字节：总长度-8字节，表明从此后面所有的数据长度，小端模式存储数据
        WAV_HeaderInfo.AddRange(Encoding.ASCII.GetBytes("WAVE")); // 4个字节：固定格式，“WAVE”对应的ASCII码，表明这个文件的格式是WAV
        WAV_HeaderInfo.AddRange(Encoding.ASCII.GetBytes("fmt ")); // 4个字节：固定格式，“fmt ”(有一个空格)对应的ASCII码，它是一个格式块标识
        WAV_HeaderInfo.AddRange(BitConverter.GetBytes(16)); // 4个字节：fmt的数据块的长度（如果没有其他附加信息，通常为16），小端模式存储数据
        var fmt_Struct = new
        {
            PCM_Code = (short)1, // 4B，编码格式代码：常见WAV文件采用PCM脉冲编码调制格式，通常为1。
            SoundChannel = (short)data_SoundCH, // 2B，声道数
            SampleRate = data_Sample, // 4B，没个通道的采样率：常见有：11025、22050、44100等
            BytesPerSec =
                data_SamplingBits * data_Sample * data_SoundCH /
                8, // 4B，数据传输速率 = 声道数×采样频率×每样本的数据位数/8。播放软件利用此值可以估计缓冲区的大小。
            BlockAlign = (short)(data_SamplingBits * data_SoundCH / 8), // 2B，采样帧大小 = 声道数×每样本的数据位数/8。
            SamplingBits = (short)data_SamplingBits // 4B，每个采样值（采样本）的位数，常见有：4、8、12、16、24、32
        };
        // 依次写入fmt数据块的数据（默认长度为16）
        WAV_HeaderInfo.AddRange(BitConverter.GetBytes(fmt_Struct.PCM_Code));
        WAV_HeaderInfo.AddRange(BitConverter.GetBytes(fmt_Struct.SoundChannel));
        WAV_HeaderInfo.AddRange(BitConverter.GetBytes(fmt_Struct.SampleRate));
        WAV_HeaderInfo.AddRange(BitConverter.GetBytes(fmt_Struct.BytesPerSec));
        WAV_HeaderInfo.AddRange(BitConverter.GetBytes(fmt_Struct.BlockAlign));
        WAV_HeaderInfo.AddRange(BitConverter.GetBytes(fmt_Struct.SamplingBits));
        /* 还 可以继续写入其他的扩展信息，那么fmt的长度计算要增加。*/

        WAV_HeaderInfo.AddRange(Encoding.ASCII.GetBytes("data")); // 4个字节：固定格式，“data”对应的ASCII码
        WAV_HeaderInfo.AddRange(BitConverter.GetBytes(data_Len)); // 4个字节：正式音频数据的长度。数据使用小端模式存放，如果是多声道，则声道数据交替存放。
        /* 到这里文件头信息填写完成，通常情况下共44个字节*/
        return WAV_HeaderInfo.ToArray();
    }

    private async void WaveStopMonitorUpdate()
    {
        // 监控是否应当停止
        if (!isPlan2Stop &&
            isPlaying &&
            AudioManager.ChannelIsStopped(ChannelType.BGM))
        {
            isPlan2Stop = true;
            if (extraTime4AllPerfect < 0)
            {
                // 足够播完 直接停止
                await ToggleStop();
            }
            else
            {
                // 不够播完 等待后停止
                await Task.Delay(double.IsNormal(extraTime4AllPerfect) ? (int)(extraTime4AllPerfect * 1000) : int.MaxValue);
                await ToggleStop();
            }
        }
    }

    
}