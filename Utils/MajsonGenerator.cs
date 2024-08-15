using MajdataEdit.Types;
using System.IO;

namespace MajdataEdit.Utils;
public static class MajsonGenerator
{
    public static async ValueTask Generate(string path, int selectedDifficulty)
    {
        var jsonStruct = new Majson()
        {
            title = SimaiProcessor.title!,
            artist = SimaiProcessor.artist!,
            level = SimaiProcessor.levels[selectedDifficulty],
            designer = SimaiProcessor.designer!,
            difficulty = SimaiProcessor.GetDifficultyText(selectedDifficulty),
            diffNum = selectedDifficulty
        };
        foreach (var note in SimaiProcessor.notelist)
        {
            note.noteList = note.getNotes();
            jsonStruct.timingList.Add(note);
        }

        using var stream = File.Create(path);
        await Serializer.Json.SerializeAsync(stream, jsonStruct);
    }
}
