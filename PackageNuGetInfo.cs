using NuGet.Versioning;
using System.Collections.Generic;

namespace DotNetPackages
{
    public class PackageNuGetInfo
    {
        public string Name { get; }
        public IEnumerable<SemanticVersion> Versions { get; }

        public PackageNuGetInfo(string name, IEnumerable<SemanticVersion> versions)
        {
            Name = name;
            Versions = versions;
        }
    }
}
