namespace Moriyama.PreviewAuth.Setup;

public interface IDatetimeProvider
{
    DateTime Now { get; }
    DateTimeOffset NowOffSet { get; }
}

public class DatetimeProvider : IDatetimeProvider
{
    public DateTime Now => DateTime.UtcNow;

    public DateTimeOffset NowOffSet => DateTimeOffset.Now;
}