using System.Runtime.Serialization;
using System.Text.Json;
using Moriyama.PreviewAuth.Options;
using Xunit.Sdk;

namespace Moriyama.PreviewAuth.Tests.Serializers;

internal class PreviewAuthOptionsSerializer : IXunitSerializer
{
    public object Deserialize(Type type, string serializedValue)
    {
        if (type != typeof(PreviewAuthOptions))
            throw new ArgumentException($"Can only deserialize {nameof(PreviewAuthOptions)}", nameof(type));

        return JsonSerializer.Deserialize<PreviewAuthOptions>(serializedValue) 
            ?? throw new SerializationException("Failed to deserialize PreviewAuthOptions");
    }

    public bool IsSerializable(Type type, object? value, out string failureReason)
    {
        if (type != typeof(PreviewAuthOptions))
        {
            failureReason = $"Can only serialize {nameof(PreviewAuthOptions)}";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public string Serialize(object value)
    {
        if (value is not PreviewAuthOptions options)
            throw new ArgumentException($"Can only serialize {nameof(PreviewAuthOptions)}", nameof(value));

        return JsonSerializer.Serialize(options);
    }
}
