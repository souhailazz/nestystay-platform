using System.Globalization;
using System.Text;

namespace NestyStay.Api.Services;

public sealed record CalendarIcsEvent(
    string Uid,
    DateOnly StartsOn,
    DateOnly EndsOn,
    string Summary,
    string Status = "CONFIRMED",
    DateTimeOffset? LastModified = null);

public static class CalendarIcsBuilder
{
    public static string Build(IEnumerable<CalendarIcsEvent> events, DateTimeOffset now)
    {
        var lines = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//NestyStay//Calendar//EN",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            $"DTSTAMP:{FormatTimestamp(now)}"
        };

        foreach (var item in events.OrderBy(item => item.StartsOn).ThenBy(item => item.Uid, StringComparer.Ordinal))
        {
            lines.Add("BEGIN:VEVENT");
            lines.Add($"UID:{Escape(item.Uid)}");
            lines.Add($"DTSTART;VALUE=DATE:{item.StartsOn:yyyyMMdd}");
            lines.Add($"DTEND;VALUE=DATE:{item.EndsOn:yyyyMMdd}");
            lines.Add($"SUMMARY:{Escape(item.Summary)}");
            lines.Add($"STATUS:{(item.Status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase) ? "CANCELLED" : "CONFIRMED")}");
            lines.Add($"DTSTAMP:{FormatTimestamp(now)}");
            lines.Add($"LAST-MODIFIED:{FormatTimestamp(item.LastModified ?? now)}");
            lines.Add("END:VEVENT");
        }

        lines.Add("END:VCALENDAR");
        return string.Join("\r\n", lines) + "\r\n";
    }

    private static string FormatTimestamp(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
}
