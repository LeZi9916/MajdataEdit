using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using System.Threading.Tasks;

namespace MajdataEdit.Utils;
public static class MessageBox
{
    public static async Task<string> ShowAsync(MessageBoxCustomParams @params)
    {
        var box = MessageBoxManager.GetMessageBoxCustom(@params);
        return await box.ShowAsync();
    }
    public static async Task<string> ShowWindowAsync(MessageBoxCustomParams @params)
    {
        var box = MessageBoxManager.GetMessageBoxCustom(@params);
        return await box.ShowWindowAsync();
    }
    public static async Task<ButtonResult> ShowAsync(MessageBoxStandardParams @params)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(@params);
        return await box.ShowAsync();
    }
    public static async Task<ButtonResult> ShowAsync(string title, string content, ButtonEnum buttons = ButtonEnum.Ok, Icon icon = Icon.None)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(title, content, buttons, icon);
        return await box.ShowAsync();
    }
    public static async Task<ButtonResult> ShowWindowAsync(string title, string content, ButtonEnum buttons = ButtonEnum.Ok, Icon icon = Icon.None)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(title, content, buttons, icon);
        return await box.ShowWindowAsync();
    }
}
