using System.Diagnostics;
using System.Security.Cryptography;
using Claunia.PropertyList;
using ivinject.Features.Command.Models;

namespace ivinject.Features.Command;

internal static class ProvisioningProfileParser
{
    internal static ProfileInfo Parse(FileInfo profile)
    {
        using var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = "security",
                ArgumentList = { "cms", "-D", "-i", profile.FullName },
                RedirectStandardOutput = true
            }
        );

        process!.WaitForExit();
        var dictionary = (NSDictionary)PropertyListParser.Parse(process.StandardOutput.BaseStream);
        
        var entitlements = (NSDictionary)dictionary["Entitlements"];
        var bundleId = ((NSString)entitlements["application-identifier"]).Content[11..];

        var certificate = ((NSData)((NSArray)dictionary["DeveloperCertificates"])[0]).Bytes;
        var identity = Convert.ToHexString(SHA1.HashData(certificate));

        var entitlementsFile = Path.GetTempFileName();
        File.WriteAllText(entitlementsFile, entitlements.ToXmlPropertyList());

        var entitlementsFileInfo = new FileInfo(entitlementsFile);

        return new ProfileInfo
        {
            BundleId = bundleId,
            Identity = identity,
            Entitlements = entitlementsFileInfo
        };
    }
}