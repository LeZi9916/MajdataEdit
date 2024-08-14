using MajdataEdit.Types;
using System.IO;
using System.Text.Json;

namespace MajdataEdit.Utils;
public static class MajsonGenerator
{
    public static async Task Generate(string path, int selectedDifficulty)
    {
        var jsonStruct = new Majson();
        foreach (var note in SimaiProcessor.notelist)
        {
            note.noteList = note.getNotes();
            jsonStruct.timingList.Add(note);
        }

        jsonStruct.title = SimaiProcessor.title!;
        jsonStruct.artist = SimaiProcessor.artist!;
        jsonStruct.level = SimaiProcessor.levels[selectedDifficulty];
        jsonStruct.designer = SimaiProcessor.designer!;
        jsonStruct.difficulty = SimaiProcessor.GetDifficultyText(selectedDifficulty);
        jsonStruct.diffNum = selectedDifficulty;

        using var stream = File.OpenWrite(path);
        await JsonSerializer.SerializeAsync(stream, jsonStruct);
    }
}
