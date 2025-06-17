using MYA.RequestProtect.Setup;

namespace MYA.RequestProtect.Tests.Setup;

internal class TestDatetimeProvider : IDatetimeProvider
{
    public DateTime Now => new(2025, 6, 10, 12, 0, 0, DateTimeKind.Utc);

    public DateTimeOffset NowOffSet => new(Now);
}
