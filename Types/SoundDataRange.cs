namespace MajdataEdit.Types;

struct SoundDataRange
{
    internal SoundDataRange(SoundDataType type, long from, long len)
    {
        Type = type;
        From = from;
        To = from + len;
    }

    public SoundDataType Type { get; }
    public long From { get; }
    public long To { get; private set; }

    public long Length
    {
        get => To - From;
        set => To = From + value;
    }

    public bool In(long value)
    {
        return value >= From && value < To;
    }
}