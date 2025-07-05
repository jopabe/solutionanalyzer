using System.Runtime.InteropServices;
using System.Text;

namespace Jox.SolutionAnalyzer;

internal static class NetFrameworkBackports
{
#if NETFRAMEWORK
    public static string GetRelativePath(string fromPath, string toPath)
    {
        var uri = new Uri(fromPath);
        var rel = Uri.UnescapeDataString(uri.MakeRelativeUri(new Uri(toPath)).ToString());
        return rel.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }
#else
    public static string GetRelativePath(string fromPath, string toPath) => Path.GetRelativePath(fromPath, toPath);

#endif
}
