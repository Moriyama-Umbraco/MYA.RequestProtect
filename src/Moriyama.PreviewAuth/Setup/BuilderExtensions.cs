using Moriyama.PreviewAuth.Options;

namespace Moriyama.PreviewAuth.Setup;

public static class BuilderExtensions
{
	public static IHostApplicationBuilder AddPreviewAuth(this IHostApplicationBuilder builder)
	{
		var t = builder.Services.AddOptions<PreviewAuthOptions>();
		t.Bind(builder.Configuration.GetSection(PreviewAuthOptions.Key));


		return builder;
	}
}
