using Umbraco.Cms.Core.Manifest;

namespace MYA.RequestProtect.Umbraco.Manifests;

#if NET8_0
internal sealed class RequestProtectManifest : IManifestFilter
{
    public void Filter(List<PackageManifest> manifests)
    {
        manifests.Add(new PackageManifest
        {
            PackageName = "Moriyama.RequestProtect.Umbraco",
            Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
            AllowPackageTelemetry = true,
            Scripts = Array.Empty<string>(),
            Stylesheets = Array.Empty<string>()
        });
    }
}

#endif