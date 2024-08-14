namespace MajdataEdit.Types;
public readonly struct ViewResponse
{
    public required ResponseCode Code { get; init; }
    public string? Response { get; init; }
    public Exception? Exception { get; init; }
    public bool IsSuccess => Code == ResponseCode.OK;
}
