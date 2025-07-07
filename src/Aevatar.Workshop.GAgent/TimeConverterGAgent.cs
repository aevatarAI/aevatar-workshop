using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Aevatar.Workshop.GAgent;

[GenerateSerializer]
public class TimeConverterGAgentState : StateBase
{
    [Id(0)] public List<string> ConversionHistory { get; set; } = [];
    [Id(1)] public string LastConversion { get; set; } = string.Empty;
}

[GenerateSerializer]
public class TimeConverterStateLogEvent : StateLogEventBase<TimeConverterStateLogEvent>;

[GenerateSerializer]
public class TimeConversionLogEvent : TimeConverterStateLogEvent
{
    [Id(0)] public string Input { get; set; } = string.Empty;
    [Id(1)] public string Result { get; set; } = string.Empty;
}

[GenerateSerializer]
public class TimeConvertEvent : EventBase
{
    [Id(0)] 
    [System.ComponentModel.Description("Time input to convert. Can be 'now' for current time, or specific times like '3:00 PM', '15:00', '2024-01-01 10:30:00'")]
    public string TimeInput { get; set; } = string.Empty;
    
    [Id(1)] 
    [System.ComponentModel.Description("Source timezone (optional). Can be timezone abbreviations like 'UTC', 'EST', 'PST', 'JST', etc. Defaults to 'UTC' if not specified")]
    public string FromTimeZone { get; set; } = string.Empty;
    
    [Id(2)] 
    [System.ComponentModel.Description("Target timezone (optional). Can be timezone abbreviations like 'UTC', 'EST', 'PST', 'JST', etc. Defaults to 'Local' if not specified")]
    public string ToTimeZone { get; set; } = string.Empty;
}

public interface ITimeConverterGAgent : IStateGAgent<TimeConverterGAgentState>
{
    Task<string> ConvertTimeAsync(string timeInput, string fromTimeZone = "", string toTimeZone = "");
    Task<string> GetTimeInZoneAsync(string timeZone);
    Task<string> CalculateTimeDifferenceAsync(string time1, string time2);
}

[GAgent("timeconverter", "tools")]
public class TimeConverterGAgent : GAgentBase<TimeConverterGAgentState, TimeConverterStateLogEvent>,
    ITimeConverterGAgent
{
    private readonly Dictionary<string, TimeZoneInfo> _commonTimeZones = new()
    {
        ["UTC"] = TimeZoneInfo.Utc,
        ["GMT"] = TimeZoneInfo.Utc,
        ["EST"] = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"),
        ["CST"] = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"),
        ["MST"] = TimeZoneInfo.FindSystemTimeZoneById("Mountain Standard Time"),
        ["PST"] = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"),
        ["CET"] = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"),
        ["JST"] = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time"),
        ["IST"] = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"),
        ["AEST"] = TimeZoneInfo.FindSystemTimeZoneById("AUS Eastern Standard Time")
    };

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "Time conversion agent that can convert times between different time zones, parse various time formats, and perform time-related calculations");
    }

    public async Task<string> ConvertTimeAsync(string timeInput, string fromTimeZone = "", string toTimeZone = "")
    {
        try
        {
            Logger.LogInformation("Converting time: {Input} from {From} to {To}", timeInput, fromTimeZone, toTimeZone);

            // Parse the input time
            var (dateTime, detectedTimeZone) = ParseTimeInput(timeInput);

            // Use detected timezone if not specified
            if (string.IsNullOrEmpty(fromTimeZone) && !string.IsNullOrEmpty(detectedTimeZone))
            {
                fromTimeZone = detectedTimeZone;
            }

            // Default to UTC if no source timezone
            if (string.IsNullOrEmpty(fromTimeZone))
            {
                fromTimeZone = "UTC";
            }

            // Default to local time if no target timezone
            if (string.IsNullOrEmpty(toTimeZone))
            {
                toTimeZone = "Local";
            }

            // Get timezone info
            var sourceTimeZone = GetTimeZoneInfo(fromTimeZone);
            var targetTimeZone = GetTimeZoneInfo(toTimeZone);

            // Convert the time
            var utcTime = TimeZoneInfo.ConvertTimeToUtc(dateTime, sourceTimeZone);
            var convertedTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, targetTimeZone);

            var result = $"{convertedTime:yyyy-MM-dd HH:mm:ss} {GetTimeZoneAbbreviation(targetTimeZone)}";

            // Log the conversion
            RaiseEvent(new TimeConversionLogEvent
            {
                Input = timeInput,
                Result = $"{fromTimeZone} → {toTimeZone}: {result}"
            });
            await ConfirmEvents();

            // Publish the result
            await PublishAsync(new RecordEvent
            {
                Message = $"Time conversion: {timeInput} ({fromTimeZone}) = {result}"
            });

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error converting time: {Input}", timeInput);
            throw new InvalidOperationException($"Failed to convert time: {timeInput}", ex);
        }
    }

    public async Task<string> GetTimeInZoneAsync(string timeZone)
    {
        try
        {
            var tzInfo = GetTimeZoneInfo(timeZone);
            var currentTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tzInfo);
            var result = $"{currentTime:yyyy-MM-dd HH:mm:ss} {GetTimeZoneAbbreviation(tzInfo)}";

            await PublishAsync(new RecordEvent
            {
                Message = $"Current time in {timeZone}: {result}"
            });

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting time in zone: {Zone}", timeZone);
            throw new InvalidOperationException($"Failed to get time in zone: {timeZone}", ex);
        }
    }

    public async Task<string> CalculateTimeDifferenceAsync(string time1, string time2)
    {
        try
        {
            var (dateTime1, _) = ParseTimeInput(time1);
            var (dateTime2, _) = ParseTimeInput(time2);

            var difference = dateTime2 - dateTime1;
            var result = FormatTimeDifference(difference);

            await PublishAsync(new RecordEvent
            {
                Message = $"Time difference between {time1} and {time2}: {result}"
            });

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calculating time difference");
            throw new InvalidOperationException("Failed to calculate time difference", ex);
        }
    }

    [EventHandler]
    public async Task HandleTimeConvertEventAsync(TimeConvertEvent eventData)
    {
        Logger.LogInformation("Received time conversion request: {Input}", eventData.TimeInput);
        await ConvertTimeAsync(eventData.TimeInput, eventData.FromTimeZone, eventData.ToTimeZone);
    }

    [EventHandler]
    public async Task HandleGreetingEventAsync(GreetingEvent eventData)
    {
        // Handle time-related requests sent as greeting events
        if (!string.IsNullOrWhiteSpace(eventData.Greeting))
        {
            Logger.LogInformation("Received time request via greeting: {Request}", eventData.Greeting);

            // Try to parse the request and determine what to do
            var request = eventData.Greeting.ToLower();

            if (request.Contains("convert") || request.Contains("time in"))
            {
                // Extract time and timezone information from the request
                await HandleNaturalLanguageTimeRequest(eventData.Greeting);
            }
            else if (request.Contains("difference"))
            {
                // Handle time difference calculation
                await HandleTimeDifferenceRequest(eventData.Greeting);
            }
            else
            {
                // Default: show current time in UTC
                await GetTimeInZoneAsync("UTC");
            }
        }
    }

    private async Task HandleNaturalLanguageTimeRequest(string request)
    {
        // Simple natural language processing for time requests
        // Examples: "convert 3pm EST to PST", "what time is it in Tokyo", etc.

        if (request.ToLower().Contains("what time") || request.ToLower().Contains("current time"))
        {
            // Extract timezone from request
            var timezone = ExtractTimeZoneFromRequest(request);
            await GetTimeInZoneAsync(timezone);
        }
        else
        {
            // Try to extract time and timezones for conversion
            var (time, fromZone, toZone) = ExtractConversionDetailsFromRequest(request);
            await ConvertTimeAsync(time, fromZone, toZone);
        }
    }

    private async Task HandleTimeDifferenceRequest(string request)
    {
        // Extract two times from the request
        // This is simplified - in real implementation, you'd use more sophisticated parsing
        var times = ExtractTimesFromRequest(request);
        if (times.Count >= 2)
        {
            await CalculateTimeDifferenceAsync(times[0], times[1]);
        }
    }

    private (DateTime dateTime, string timeZone) ParseTimeInput(string input)
    {
        input = input.Trim();
        string detectedTimeZone = "";

        // Handle special case: "now"
        if (input.Equals("now", StringComparison.OrdinalIgnoreCase))
        {
            return (DateTime.Now, "Local");
        }

        // Check for timezone abbreviations at the end
        foreach (var tz in _commonTimeZones.Keys)
        {
            if (input.EndsWith($" {tz}", StringComparison.OrdinalIgnoreCase))
            {
                detectedTimeZone = tz;
                input = input.Substring(0, input.Length - tz.Length - 1).Trim();
                break;
            }
        }

        // Try to parse the datetime
        DateTime dateTime;
        if (!DateTime.TryParse(input, out dateTime))
        {
            // Try parsing with various formats
            string[] formats =
            {
                "yyyy-MM-dd HH:mm:ss",
                "MM/dd/yyyy HH:mm:ss",
                "dd/MM/yyyy HH:mm:ss",
                "yyyy-MM-dd",
                "MM/dd/yyyy",
                "HH:mm:ss",
                "HH:mm",
                "h:mm tt",
                "h:mmtt"
            };

            if (!DateTime.TryParseExact(input, formats, CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out dateTime))
            {
                // If still can't parse, assume it's a time today
                if (DateTime.TryParseExact(input, new[] { "HH:mm", "h:mm tt", "h:mmtt" },
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeOnly))
                {
                    dateTime = DateTime.Today.Add(timeOnly.TimeOfDay);
                }
                else
                {
                    throw new ArgumentException($"Unable to parse time input: {input}");
                }
            }
        }

        return (dateTime, detectedTimeZone);
    }

    private TimeZoneInfo GetTimeZoneInfo(string timeZone)
    {
        if (string.IsNullOrEmpty(timeZone) || timeZone.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            return TimeZoneInfo.Local;
        }

        if (_commonTimeZones.TryGetValue(timeZone.ToUpper(), out var tzInfo))
        {
            return tzInfo;
        }

        // Try to find by ID
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        }
        catch
        {
            // Try to find by display name
            var allTimeZones = TimeZoneInfo.GetSystemTimeZones();
            var found = allTimeZones.FirstOrDefault(tz =>
                tz.DisplayName.Contains(timeZone, StringComparison.OrdinalIgnoreCase) ||
                tz.StandardName.Contains(timeZone, StringComparison.OrdinalIgnoreCase));

            if (found != null)
            {
                return found;
            }

            throw new ArgumentException($"Unknown timezone: {timeZone}");
        }
    }

    private string GetTimeZoneAbbreviation(TimeZoneInfo timeZone)
    {
        // Return known abbreviations or generate from the name
        var known = _commonTimeZones.FirstOrDefault(kvp => kvp.Value.Id == timeZone.Id);
        if (!string.IsNullOrEmpty(known.Key))
        {
            return known.Key;
        }

        // Generate abbreviation from standard name
        var words = timeZone.StandardName.Split(' ');
        return string.Join("", words.Select(w => w.Length > 0 ? w[0].ToString() : ""));
    }

    private string FormatTimeDifference(TimeSpan difference)
    {
        var parts = new List<string>();

        if (Math.Abs(difference.Days) > 0)
            parts.Add($"{Math.Abs(difference.Days)} day{(Math.Abs(difference.Days) != 1 ? "s" : "")}");

        if (Math.Abs(difference.Hours) > 0)
            parts.Add($"{Math.Abs(difference.Hours)} hour{(Math.Abs(difference.Hours) != 1 ? "s" : "")}");

        if (Math.Abs(difference.Minutes) > 0)
            parts.Add($"{Math.Abs(difference.Minutes)} minute{(Math.Abs(difference.Minutes) != 1 ? "s" : "")}");

        if (Math.Abs(difference.Seconds) > 0 && difference.Days == 0)
            parts.Add($"{Math.Abs(difference.Seconds)} second{(Math.Abs(difference.Seconds) != 1 ? "s" : "")}");

        var result = string.Join(", ", parts);

        if (difference.TotalSeconds < 0)
            result = $"{result} ago";
        else
            result = $"{result} ahead";

        return result;
    }

    private string ExtractTimeZoneFromRequest(string request)
    {
        // Simple extraction - look for timezone names
        foreach (var tz in _commonTimeZones.Keys)
        {
            if (request.Contains(tz, StringComparison.OrdinalIgnoreCase))
            {
                return tz;
            }
        }

        // Look for city names
        var cityTimeZones = new Dictionary<string, string>
        {
            ["tokyo"] = "JST",
            ["london"] = "GMT",
            ["new york"] = "EST",
            ["los angeles"] = "PST",
            ["chicago"] = "CST",
            ["denver"] = "MST",
            ["paris"] = "CET",
            ["mumbai"] = "IST",
            ["sydney"] = "AEST"
        };

        foreach (var city in cityTimeZones)
        {
            if (request.Contains(city.Key, StringComparison.OrdinalIgnoreCase))
            {
                return city.Value;
            }
        }

        return "UTC";
    }

    private (string time, string fromZone, string toZone) ExtractConversionDetailsFromRequest(string request)
    {
        // This is a simplified implementation
        // In production, you'd use more sophisticated NLP or regex patterns

        string time = "now";
        string fromZone = "";
        string toZone = "";

        // Look for time patterns
        var timeMatch = System.Text.RegularExpressions.Regex.Match(request, @"\b\d{1,2}(:\d{2})?\s*(am|pm)?\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (timeMatch.Success)
        {
            time = timeMatch.Value;
        }

        // Look for "from X to Y" pattern
        var conversionMatch = System.Text.RegularExpressions.Regex.Match(request, @"from\s+(\w+)\s+to\s+(\w+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (conversionMatch.Success)
        {
            fromZone = conversionMatch.Groups[1].Value;
            toZone = conversionMatch.Groups[2].Value;
        }

        return (time, fromZone, toZone);
    }

    private List<string> ExtractTimesFromRequest(string request)
    {
        var times = new List<string>();

        // Look for time patterns
        var matches = System.Text.RegularExpressions.Regex.Matches(request,
            @"\b\d{1,2}(:\d{2})?\s*(am|pm)?\b|\b\d{4}-\d{2}-\d{2}\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            times.Add(match.Value);
        }

        return times;
    }

    protected override void GAgentTransitionState(TimeConverterGAgentState state,
        StateLogEventBase<TimeConverterStateLogEvent> @event)
    {
        switch (@event)
        {
            case TimeConversionLogEvent conversion:
                state.ConversionHistory.Add($"{conversion.Input} → {conversion.Result}");
                state.LastConversion = conversion.Result;
                break;
        }
    }
}