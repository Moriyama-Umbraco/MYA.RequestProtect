using Xunit.Sdk;
using MYA.RequestProtect.Options;
using MYA.RequestProtect.Tests.Serializers;

[assembly: RegisterXunitSerializer(typeof(RequestProtectOptionsSerializer),
    typeof(RequestProtectOptions))]
