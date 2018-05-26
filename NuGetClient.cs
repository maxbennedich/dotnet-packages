using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DotNetPackages
{
    public class NuGetClient
    {
        public async Task<PackageNuGetInfo> GetPackageInfo(string packageName, Action postProcessingAction)
        {
            JObject json = await GetResource($"{packageName.ToLower()}/index.json");
            var versions = new List<SemanticVersion>();

            var items = json["items"].AsJEnumerable();
            if (items.Count() == 1)
            {
                versions.AddRange(ExtractVersions(items.ElementAt(0)["items"]));
            }
            else
            {
                // support two cases: 1) in-page catalog pages 2) external catalog pages
                var externalPages = new List<JToken>();

                foreach (var item in items)
                {
                    JToken subItems = item["items"];
                    if (subItems != null)
                        versions.AddRange(ExtractVersions(subItems)); // items present in page
                    else
                        externalPages.Add(item); // items in external file, need to load separately
                }

                var requests = externalPages.Select(item => {
                    string id = item["@id"].ToString();
                    string resourceName = id.Substring(id.IndexOf(packageName.ToLower()));
                    return GetResource(resourceName);
                });

                var pages = await Task.WhenAll(requests);
                foreach (JObject page in pages)
                    versions.AddRange(ExtractVersions(page["items"]));
            }

            // put latest versions first
            versions.Reverse();

            postProcessingAction();

            return new PackageNuGetInfo(packageName, versions);
        }

        private async Task<JObject> GetResource(string name)
        {
            var request = WebRequest.Create($"https://api.nuget.org/v3/registration3/{name}");
            request.Proxy = null;

            // force IPv4 address due to the IPv6 version of "api.nuget.org" sometimes not resolving 
            ((HttpWebRequest)request).ServicePoint.BindIPEndPointDelegate = (servicePount, remoteEndPoint, retryCount) =>
            {
                if (remoteEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return new IPEndPoint(IPAddress.Any, 0);
                throw new InvalidOperationException("No IPv4 address");
            };

            WebResponse response = await request.GetResponseAsync();
            using (var reader = new StreamReader(response.GetResponseStream()))
                return JObject.Parse(reader.ReadToEnd());
        }

        private IEnumerable<SemanticVersion> ExtractVersions(JToken items)
        {
            foreach (JToken item in items)
            {
                bool listed = Convert.ToBoolean(item["catalogEntry"]["listed"].ToString());
                if (!listed)
                    continue;

                if (SemanticVersion.TryParse(item["catalogEntry"]["version"].ToString(), out SemanticVersion version))
                    yield return version;
            }
        }
    }
}
