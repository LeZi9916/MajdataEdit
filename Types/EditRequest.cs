using MajdataEdit.Interfaces;

namespace MajdataEdit.Types;

internal readonly struct EditRequest : IEditRequest
{
    public float? AudioSpeed { get; init; }
    public float? BackgroundCover { get; init; }
    public EditorComboIndicator? ComboStatusType { get; init; }
    public EditorPlayMethod? EditorPlayMethod { get; init; }
    public required EditorControlMethod Control { get; init; }
    public string? JsonPath { get; init; }
    public float? NoteSpeed { get; init; }
    public long? StartAt { get; init; }
    public float? StartTime { get; init; }
    public float? TouchSpeed { get; init; }
    public bool? SmoothSlideAnime { get; init; }
}
