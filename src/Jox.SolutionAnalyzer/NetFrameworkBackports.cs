namespace Jox.SolutionAnalyzer;

#if NETFRAMEWORK
internal static class NetFrameworkBackports
{
    extension(Path)
    {
        public static string GetRelativePath(string fromPath, string toPath)
        {
            if (fromPath.Last() != Path.DirectorySeparatorChar)
            {
                fromPath += Path.DirectorySeparatorChar;
            }
            var uri = new Uri(fromPath);
            var rel = Uri.UnescapeDataString(uri.MakeRelativeUri(new Uri(toPath)).ToString());
            return rel.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }
    }
}
#endif
