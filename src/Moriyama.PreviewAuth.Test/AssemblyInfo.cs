using Xunit.Sdk;
using Moriyama.PreviewAuth.Options;
using Moriyama.PreviewAuth.Tests.Serializers;

[assembly: RegisterXunitSerializer(typeof(PreviewAuthOptionsSerializer),
    typeof(PreviewAuthOptions))]