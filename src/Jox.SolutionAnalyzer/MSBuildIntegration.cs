using Microsoft.Build.Locator;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Jox.SolutionAnalyzer;

internal class MSBuildIntegration
{
    public static void RegisterMSBuildLocation(DirectoryInfo? path)
    {
        if (!MSBuildLocator.IsRegistered)
        {
            if ((path == null || !path.Exists) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // when running in net6.0, MSBuildLocator.QueryVisualStudioInstances() can't find
                // MSBuild versions that support building pre-SDK projects, so use vswhere
                var latestVisualStudio = DetectLatestVisualStudio();
                if (latestVisualStudio != null)
                {
                    path = new DirectoryInfo(Path.Combine(latestVisualStudio, @"Msbuild\Current\Bin"));
                }
            }
            if (path != null && path.Exists)
            {
                MSBuildLocator.RegisterMSBuildPath(path.FullName);
            }
            else
            {
                var latest = MSBuildLocator.QueryVisualStudioInstances().MaxBy(i => i.Version);
                MSBuildLocator.RegisterInstance(latest);
            }
        }
    }

    private static string? DetectLatestVisualStudio()
    {
        try
        {
            var vswhereInfo = new ProcessStartInfo()
            {
                FileName = @"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe",
                Arguments = "-requires Microsoft.Component.MSBuild -property installationPath -latest -prerelease -version [17.0,",
                RedirectStandardOutput = true
            };
            if (!File.Exists(vswhereInfo.FileName)) return null;

            using var vswhere = Process.Start(vswhereInfo);
            if (vswhere == null) return null;
            vswhere.WaitForExit();
            if (vswhere.ExitCode != 0) return null;
            return vswhere!.StandardOutput.ReadToEnd().Trim();
        }
        catch (Exception)
        {
            // no worries
            return null;
        }
    }
}
