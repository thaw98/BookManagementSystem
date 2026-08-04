using System.Globalization;

namespace Contracts.Notification;

public static class NotificationDateTime
{
    public const string DisplayFormat = "dd/MM/yyyy h:mm tt";

    private static readonly TimeZoneInfo MyanmarTimeZone = ResolveMyanmarTimeZone();

    public static string Format(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return TimeZoneInfo.ConvertTimeFromUtc(utc, MyanmarTimeZone)
            .ToString(DisplayFormat, CultureInfo.InvariantCulture);
    }

    private static TimeZoneInfo ResolveMyanmarTimeZone()
    {
        foreach (var id in new[] { "Asia/Yangon", "Myanmar Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        throw new TimeZoneNotFoundException(
            "Neither the Asia/Yangon nor Myanmar Standard Time time zone is available.");
    }
}
