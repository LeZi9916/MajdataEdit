using MajdataEdit.Types;
using System.IO;
using System.Text.Json;

namespace MajdataEdit;
public static class MajsonGenerator
{
    public static async Task Generate(string path,int selectedDifficulty)
    {
        var jsonStruct = new Majson();
        foreach (var note in SimaiProcess.notelist)
        {
            note.noteList = note.getNotes();
            jsonStruct.timingList.Add(note);
        }

        jsonStruct.title = SimaiProcess.title!;
        jsonStruct.artist = SimaiProcess.artist!;
        jsonStruct.level = SimaiProcess.levels[selectedDifficulty];
        jsonStruct.designer = SimaiProcess.designer!;
        jsonStruct.difficulty = SimaiProcess.GetDifficultyText(selectedDifficulty);
        jsonStruct.diffNum = selectedDifficulty;

        using var stream = File.OpenWrite(path);
        await JsonSerializer.SerializeAsync(stream,jsonStruct);
    }
}
