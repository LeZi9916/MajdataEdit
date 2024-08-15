using MajdataEdit.Utils;

namespace MajdataEdit.Types;

internal class Majson
{
    public string artist { get; init; } = "default";
    public string designer { get; init; } = "default";
    public string difficulty { get; init; } = "EZ";
    public int diffNum { get; init; } = 0;
    public string level { get; init; } = "1";
    public List<SimaiTimingPoint> timingList { get; init; } = new();
    public string title { get; init; } = "default";
}