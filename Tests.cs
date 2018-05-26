using System.Linq;
using NuGet.Versioning;
using NUnit.Framework;

namespace DotNetPackages
{
    [TestFixture]
    public class Tests
    {
        private static Package Current(string version)
        {
            return new Package("Test.Package", version, "TestProject");
        }

        private static PackageNuGetInfo Available(params string[] versions)
        {
            return new PackageNuGetInfo("Test.Package", versions.Select(v => SemanticVersion.Parse(v)));
        }

        private void AssertVersions(PackageStatus status, AnsiColor color, string wanted, string stable, string latest)
        {
            Assert.AreEqual(color, status.GetStatusColor());
            Assert.AreEqual(wanted, status.WantedVersion.ToString());
            Assert.AreEqual(stable, status.StableVersion?.ToString());
            Assert.AreEqual(latest, status.LatestVersion.ToString());
        }

        [Test]
        public void TestNoLaterStable()
        {
            var status = PackageStatus.GetStatus(Current("3.2.1"), Available("3.2.2-preview", "3.2.1", "3.2.0"));
            AssertVersions(status, AnsiColor.White, "3.2.1", "3.2.1", "3.2.2-preview");
        }

        [Test]
        public void TestLaterStable()
        {
            var status = PackageStatus.GetStatus(Current("3.2.1"), Available("3.2.2", "3.2.1", "3.2.0"));
            AssertVersions(status, AnsiColor.Yellow, "3.2.2", "3.2.2", "3.2.2");
        }

        [Test]
        public void TestLaterMajor()
        {
            var status = PackageStatus.GetStatus(Current("3.2.1"), Available("4.0.0", "3.2.2", "3.2.1"));
            AssertVersions(status, AnsiColor.Orange, "3.2.2", "4.0.0", "4.0.0");
        }

        [Test]
        public void TestLaterStableWithCurrentPrelease()
        {
            var status = PackageStatus.GetStatus(Current("3.2.1-alpha"), Available("3.2.1", "3.2.1-alpha", "3.2.0"));
            AssertVersions(status, AnsiColor.Orange, "3.2.1", "3.2.1", "3.2.1");
        }

        [Test]
        public void TestLaterPrelease()
        {
            var status = PackageStatus.GetStatus(Current("3.2.1-alpha"), Available("3.2.1-beta", "3.2.1-alpha", "3.2.0"));
            AssertVersions(status, AnsiColor.Yellow, "3.2.1-beta", "3.2.0", "3.2.1-beta");
        }

        [Test]
        public void TestNoStable()
        {
            var status = PackageStatus.GetStatus(Current("3.2.1-alpha"), Available("3.2.1-beta", "3.2.1-alpha"));
            AssertVersions(status, AnsiColor.Yellow, "3.2.1-beta", null, "3.2.1-beta");
        }
    }
}