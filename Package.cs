using NuGet.Versioning;
using System.Text.RegularExpressions;

namespace DotNetPackages
{
    public class Package
    {
        private static Regex ProjectNameRegex = new Regex("[\\\\/]([^\\\\/]+)\\.[cf]sproj", RegexOptions.Singleline | RegexOptions.Compiled);

        public string Name { get; }
        public SemanticVersion CurrentVersion { get; }
        public string ProjectFileName { get; }
        public string ProjectName { get; }

        public Package(string name, string version, string projectFileName)
        {
            Name = name;
            CurrentVersion = SemanticVersion.Parse(version);
            ProjectFileName = projectFileName;
            ProjectName = GetProjectName(projectFileName);
        }

        private static string GetProjectName(string projectFile)
        {
            return ProjectNameRegex.Match(projectFile).Groups[1].Value;
        }
    }
}
