using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit.Utils;
public static class Platform
{
    public static void OpenUrl(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var proc = new Process { StartInfo = { UseShellExecute = true, FileName = url } };
            proc.Start();

            return;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("x-www-browser", url);
            return;
        }
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            throw new Exception("invalid url: " + url);
        Process.Start("open", url);
    }
}
