using System.Diagnostics;
using System.IO;
using Un4seen.Bass;

namespace MajdataEdit.Types;

class SoundBank
{
    internal SoundBank(string Path)
    {
        FilePath = Path;

        InitializeSampleData();
    }

    public bool Temp { get; private set; }
    public string FilePath { get; private set; }
    public int ID { get; private set; }
    public BASS_SAMPLE? Info { get; private set; }

    public long RawSize { get; set; }
    public short[]? Raw { get; private set; }

    public int Frequency
    {
        get
        {
            if (Info != null) return Info.freq;
            return -1;
        }
    }

    public void Reassign(string FFMpegDirectory, string NewDirectory, string Filename, int NewFrequency)
    {
        if (FFMpegDirectory.Length == 0)
            return;

        Func<string, string> NormalizePath = path =>
        {
            return string.Join(Path.DirectorySeparatorChar.ToString(), path.Split('/'));
        };

        Temp = true;
        var OriginalPath = FilePath;
        FilePath = NewDirectory + "/" + Filename;

        var args = string.Format(
            "-loglevel 24 -y -i \"{0}\" -ac 2 -ar {2} \"{1}\"",
            NormalizePath(OriginalPath),
            NormalizePath(FilePath),
            NewFrequency
        );
        var startInfo = new ProcessStartInfo(FFMpegDirectory + "/ffmpeg.exe", args)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true
        };
        var proc = Process.Start(startInfo)!;
        proc.WaitForExit();
        if (proc.ExitCode != 0)
            throw new Exception(proc.StandardError.ReadToEnd());

        Free();
        InitializeSampleData();
    }

    private void InitializeSampleData()
    {
        ID = Bass.BASS_SampleLoad(FilePath, 0, 0, 1, BASSFlag.BASS_DEFAULT);
        if (ID != 0)
            Info = Bass.BASS_SampleGetInfo(ID);

        if (Info != null)
            RawSize = Info.length / 2;
        else
            RawSize = 0;
    }

    public void InitializeRawSample()
    {
        if (Info == null)
            return;

        Raw = new short[RawSize];
        Bass.BASS_SampleGetData(ID, Raw);
    }

    public void Free()
    {
        if (ID <= 0)
            return;

        Raw = null;
        Bass.BASS_SampleFree(ID);
    }

    public bool FrequencyCheck(SoundBank other)
    {
        return Frequency == other.Frequency && Frequency > 0;
    }
}