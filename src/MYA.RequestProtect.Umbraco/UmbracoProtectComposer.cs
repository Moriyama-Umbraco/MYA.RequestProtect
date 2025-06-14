using MYA.RequestProtect.Setup;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace MYA.RequestProtect.Umbraco;

public class UmbracoProtectComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddRequestProtect()
            .Configure<UmbracoPipelineOptions>(options =>
                options.AddFilter(new UmbracoPipelineFilter("RequestProtect", prePipeline: app => app.UseRequestProtect())));
    }
}
