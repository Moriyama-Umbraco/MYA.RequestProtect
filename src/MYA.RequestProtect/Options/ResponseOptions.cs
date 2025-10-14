using MYA.RequestProtect.Enums;

namespace MYA.RequestProtect.Options;

public class ResponseOptions
{
    public ResponseTypes? ResponseType { get; set; }

    public string? Destination { get; set; }

    public int StatusCode { get; set; } = 400;
}
