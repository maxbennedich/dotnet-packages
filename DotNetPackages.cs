using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DotNetPackages
{
    /// <summary>
    /// This tool finds all packages referenced in all csproj and fsproj files in the current directory and all its subdirectories (recursively),
    /// and then connects to NuGet to find which packages have updates available. Only .NET Core project files are supported.
    /// <para/>
    /// This tool plugs into the dotnet CLI toolchain by putting the executable ("dotnet-packages") in the path, then running "dotnet packages".
    /// <para/>
    /// Based on https://github.com/goenning/dotnet-outdated
    /// </summary>
    class DotNetPackages
    {
        private static readonly Regex PackageRegex = new Regex("Include=\"(.+?)\" Version=\"(.+?)\"", RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly object ConsoleLock = new object();

        static void Main(string[] args)
        {
            // Command window on Windows has ANSI disabled by default
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                ConsoleUtils.EnableANSI();

            // Prevent proxy-selection timeout
            WebRequest.DefaultWebProxy = null;

            new DotNetPackages().Run();
        }

        private void Run()
        {
            List<Package> packages = GetAllProjectPackages();

            PackageNuGetInfo[] packageInfos = DownloadPackageInfo(packages);

            Console.WriteLine();

            PrintPackageStatus(packages, packageInfos);
        }

        /// <summary>Adds all packages found in all csproj and fsproj files in the current directory and all its subdirectories (recursively).</summary>
        private List<Package> GetAllProjectPackages()
        {
            var packages = new List<Package>();

            foreach (string projFile in Directory.GetFiles(".", "*.?sproj", SearchOption.AllDirectories))
            {
                foreach (string packageLine in File.ReadLines(projFile).Where(line => line.Contains("PackageReference Include=")))
                {
                    Match match = PackageRegex.Match(packageLine);
                    if (match.Groups.Count != 3)
                    {
                        Console.WriteLine($"Unexpected content in file {projFile}: {packageLine}");
                        continue;
                    }

                    string package = match.Groups[1].Value;
                    string version = match.Groups[2].Value;

                    try
                    {
                        packages.Add(new Package(package, version, projFile));
                    }
                    catch (Exception ae)
                    {
                        Console.WriteLine($"Failed to parse package in file {projFile}: name={package} version={version} ({ae.Message.Replace(System.Environment.NewLine, " ")})");
                    }
                }
            }

            return packages;
        }

        private PackageNuGetInfo[] DownloadPackageInfo(List<Package> packages)
        {
            var uniquePackages = new HashSet<string>(packages.Select(p => p.Name));
            int packageCount = uniquePackages.Count;

            if (packageCount == 0)
            {
                Console.WriteLine($"No packages found in {Directory.GetCurrentDirectory()}");
                return new PackageNuGetInfo[0];
            }

            string progressBarTitle = "Retrieving packages: ";
            int startColumn = progressBarTitle.Length + 1;
            Console.Write($"{progressBarTitle}[{AnsiUtils.CursorForward(packageCount)}] 0/{packageCount}");

            int downloadedPackages = 0;
            var nuGetClient = new NuGetClient();
            var requests = uniquePackages.Select(p => nuGetClient.GetPackageInfo(p, () =>
            {
                // update progress bar for each downloaded package info
                lock (ConsoleLock)
                {
                    ++downloadedPackages;
                    Console.Write($"{AnsiUtils.Cursor(startColumn + downloadedPackages)}={AnsiUtils.Cursor(startColumn + packageCount + 3)}{downloadedPackages}/{packageCount}");
                }
            }));
            var packageInfos = Task.WhenAll(requests).Result;

            Console.WriteLine(); // end of line for progress bar

            return packageInfos;
        }

        private void PrintPackageStatus(List<Package> packages, PackageNuGetInfo[] packageInfos)
        {
            var packageInfoByName = packageInfos.ToDictionary(packageInfo => packageInfo.Name);

            var statuses = packages.Select(package => PackageStatus.GetStatus(package, packageInfoByName[package.Name]));

            var inconsistentVersions = new HashSet<string>();
            foreach (var group in statuses.GroupBy(status => status.PackageNuGetInfo.Name))
                if (group.Select(package => package.Package.CurrentVersion).Distinct().Count() > 1)
                    inconsistentVersions.Add(group.Key);

            var orderedStatuses = statuses.OrderBy(status => status.PackageNuGetInfo.Name).ThenBy(status => status.Package.ProjectName);

            TableDrawer.DrawTable(orderedStatuses,
                new[] { "Package", "Project", "Current", "Wanted", "Stable", "Latest"},
                status =>
                {
                    // Different versions of one and the same package detected
                    if (inconsistentVersions.Contains(status.PackageNuGetInfo.Name))
                        return AnsiColor.Red;

                    return status.GetStatusColor();
                },
                status => status.PackageNuGetInfo.Name,
                status => status.Package.ProjectName,
                status => status.Package.CurrentVersion,
                status => status.WantedVersion,
                status => status.StableVersion,
                status => status.LatestVersion
            );
        }
    }
}
