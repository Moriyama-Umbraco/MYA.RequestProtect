using System.Runtime.Serialization;
using System.Text.Json;
using MYA.RequestProtect.Options;
using Xunit.Sdk;

namespace MYA.RequestProtect.Tests.Serializers;

internal class RequestProtectOptionsSerializer : IXunitSerializer
{
    public object Deserialize(Type type, string serializedValue)
    {
        if (type != typeof(RequestProtectOptions))
            throw new ArgumentException($"Can only deserialize {nameof(RequestProtectOptions)}", nameof(type));

        return JsonSerializer.Deserialize<RequestProtectOptions>(serializedValue) 
            ?? throw new SerializationException("Failed to deserialize RequestProtectOptions");
    }

    public bool IsSerializable(Type type, object? value, out string failureReason)
    {
        if (type != typeof(RequestProtectOptions))
        {
            failureReason = $"Can only serialize {nameof(RequestProtectOptions)}";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public string Serialize(object value)
    {
        if (value is not RequestProtectOptions options)
            throw new ArgumentException($"Can only serialize {nameof(RequestProtectOptions)}", nameof(value));

        return JsonSerializer.Serialize(options);
    }
}
