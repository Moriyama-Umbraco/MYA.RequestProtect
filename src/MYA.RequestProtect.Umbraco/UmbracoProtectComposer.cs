using Microsoft.Extensions.DependencyInjection;
using MYA.RequestProtect.Setup;
#if NET8_0
using MYA.RequestProtect.Umbraco.Manifests;
#endif
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace MYA.RequestProtect.Umbraco;

public class UmbracoProtectComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        #if NET8_0
        builder.ManifestFilters().Append<RequestProtectManifest>();
        #endif

        builder.Services.AddRequestProtect()
            .Configure<UmbracoPipelineOptions>(options =>
                options.AddFilter(new UmbracoPipelineFilter("RequestProtect", prePipeline: app => app.UseRequestProtect())));
            
    }
}
