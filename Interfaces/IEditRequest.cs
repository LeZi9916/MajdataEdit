using MajdataEdit.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit.Interfaces;
public interface IEditRequest
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
