using Semver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit;
public static class MajEnvironment
{
    public static string VersionStr { get; } = $"v{Assembly.GetExecutingAssembly().GetName().Version!.ToString(3)}";
    public static SemVersion SemVersion { get; } = SemVersion.Parse(VersionStr, SemVersionStyles.Any);
    public static Version Version { get; } = SemVersion.ToVersion();
}
