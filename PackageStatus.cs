using NuGet.Versioning;

namespace DotNetPackages
{
    public class PackageStatus
    {
        public Package Package { get; }
        public PackageNuGetInfo PackageNuGetInfo { get; }
        public SemanticVersion WantedVersion { get; private set; }
        public SemanticVersion StableVersion { get; private set; }
        public SemanticVersion LatestVersion { get; private set; }

        private PackageStatus(Package package, PackageNuGetInfo packageNuGetInfo)
        {
            Package = package;
            PackageNuGetInfo = packageNuGetInfo;
        }

        public static PackageStatus GetStatus(Package package, PackageNuGetInfo packageNuGetInfo)
        {
            var status = new PackageStatus(package, packageNuGetInfo);

            foreach (var version in packageNuGetInfo.Versions)
            {
                if (status.LatestVersion == null)
                    status.LatestVersion = version;

                if (status.StableVersion == null && !version.IsPrerelease)
                    status.StableVersion = version;

                if (status.WantedVersion == null && version.Major == package.CurrentVersion.Major && (package.CurrentVersion.IsPrerelease || !version.IsPrerelease))
                    status.WantedVersion = version;

                if (status.LatestVersion != null && status.StableVersion != null && status.WantedVersion != null)
                    break;
            }

            if (status.WantedVersion == null)
                status.WantedVersion = package.CurrentVersion;

            return status;
        }

        public AnsiColor GetStatusColor()
        {
            // New major stable version available
            if (Package.CurrentVersion.Major < StableVersion?.Major)
                return AnsiColor.Orange;

            if (Package.CurrentVersion < WantedVersion)
            {
                // Upgrade to non-prerelease available
                if (Package.CurrentVersion.IsPrerelease && !WantedVersion.IsPrerelease)
                    return AnsiColor.Orange;

                // Minor upgrade available
                return AnsiColor.Yellow;
            }

            // No upgrade available
            return AnsiColor.White;
        }
    }
}
