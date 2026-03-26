using System.Net.Http.Json;
using Newtonsoft.Json;

namespace YeeMotion;

public class SunlightService
{
    private readonly string _lat;
    private readonly string _lng;
    private readonly HttpClient _httpClient = new();

    private DateOnly _cachedDate;
    private SunTimes? _cachedTimes;

    public SunlightService(string lat, string lng)
    {
        _lat = lat;
        _lng = lng;
    }

    public async Task<bool> IsDaylight()
    {
        var times = await GetSunTimes();
        if (times is null)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        return now >= times.Sunrise && now <= times.Sunset;
    }

    private async Task<SunTimes?> GetSunTimes()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (_cachedTimes is not null && _cachedDate == today)
        {
            return _cachedTimes;
        }

        try
        {
            var url = $"https://api.sunrise-sunset.org/json?lat={_lat}&lng={_lng}&formatted=0&date={today:yyyy-MM-dd}";
            var response = await _httpClient.GetStringAsync(url);
            var result = JsonConvert.DeserializeObject<SunResponse>(response);

            if (result?.Status != "OK" || result.Results is null)
            {
                Console.WriteLine($"Sunrise API returned status: {result?.Status}");
                return _cachedTimes;
            }

            _cachedDate = today;
            _cachedTimes = result.Results;
            Console.WriteLine($"Sun times for {today}: sunrise {_cachedTimes.Sunrise:HH:mm} UTC, sunset {_cachedTimes.Sunset:HH:mm} UTC");

            return _cachedTimes;
        }
        catch (Exception exc)
        {
            Console.WriteLine($"Failed to fetch sun times: {exc.Message}");
            return _cachedTimes;
        }
    }

    private class SunResponse
    {
        public string? Status { get; set; }
        public SunTimes? Results { get; set; }
    }
}

public class SunTimes
{
    public DateTime Sunrise { get; set; }
    public DateTime Sunset { get; set; }

    [JsonProperty("civil_twilight_begin")]
    public DateTime CivilTwilightBegin { get; set; }

    [JsonProperty("civil_twilight_end")]
    public DateTime CivilTwilightEnd { get; set; }
}
