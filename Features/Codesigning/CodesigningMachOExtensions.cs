using System.Diagnostics;
using ivinject.Common.Models;
using ivinject.Features.Packaging.Models;
using static ivinject.Features.Packaging.Models.DirectoryNames;

namespace ivinject.Features.Codesigning;

internal static class CodesigningMachOExtensions
{
    internal static bool IsMainExecutable(this IviMachOBinary binary, IviDirectoriesInfo directoriesInfo)
    {
        return !Path.GetRelativePath(
            directoriesInfo.BundleDirectory,
            binary.FullName
        ).Contains(FrameworksDirectoryName);
    }
    
    internal static async Task<bool> SignAsync(
        this IviMachOBinary binary,
        string identity,
        FileInfo? entitlements = null
    )
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "codesign",
            ArgumentList = { "-s", identity },
            RedirectStandardOutput = true
        };

        if (entitlements is not null)
        {
            startInfo.ArgumentList.Add("--entitlements");
            startInfo.ArgumentList.Add(entitlements.FullName);
        }

        startInfo.ArgumentList.Add(binary.FullName);

        using var process = Process.Start(startInfo);

        await process!.WaitForExitAsync();
        return process.ExitCode == 0;
    }
    
    internal static async Task<bool> RemoveSignatureAsync(this IviMachOBinary binary)
    {
        using var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = "codesign",
                ArgumentList = { "--remove-signature", binary.FullName }
            }
        );
        
        await process!.WaitForExitAsync();
        return process.ExitCode == 0;
    }
    
    internal static async Task<bool> DumpEntitlementsAsync(this IviMachOBinary binary, string outputFilePath)
    {
        using var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = "codesign",
                ArgumentList = { "-d", "--entitlements", outputFilePath, "--xml", binary.FullName },
                RedirectStandardError = true
            }
        );

        var error = await process!.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        return process.ExitCode == 0 && error.Count(c => c.Equals('\n')) == 1
            && new FileInfo(outputFilePath).Length > 8;
    }
}