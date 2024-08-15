using MajdataEdit.Interfaces;
using MajdataEdit.Types;
using System.Diagnostics;

namespace MajdataEdit.Utils;

public static class ViewMonitor
{
    public static bool IsAvailable { get; private set; } = false;
    public static int Latency { get; private set; } = -1;

    static bool isRunning = false;
    static Stopwatch sw = new();
    public static async void Init()
    {
        if (isRunning)
            return;
        isRunning = true;
        while (true) 
        {
            sw.Restart();
            try
            {
                var rsp = await WebControl.RequestPostAsync("http://localhost:8013/Ping/",new PingRequest());
                if (rsp.IsSuccess && rsp.Response?.Trim() == "Pong")
                    IsAvailable = true;
                else
                    IsAvailable = false;
                sw.Stop();
                Latency = (int)sw.ElapsedMilliseconds;
                await Task.Delay(50);
            }
            catch
            {
                IsAvailable = false;
                await Task.Delay(50);
            }
        }
    }
    struct PingRequest : IEditRequest
    {
        public float? AudioSpeed { get; init; }
        public float? BackgroundCover { get; init; }
        public EditorComboIndicator? ComboStatusType { get; init; }
        public EditorPlayMethod? EditorPlayMethod { get; init; }
        public EditorControlMethod Control { get; init; }
        public string? JsonPath { get; init; }
        public float? NoteSpeed { get; init; }
        public long? StartAt { get; init; }
        public float? StartTime { get; init; }
        public float? TouchSpeed { get; init; }
        public bool? SmoothSlideAnime { get; init; }
    }
}
