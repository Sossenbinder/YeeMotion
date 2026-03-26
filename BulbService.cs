using Microsoft.Extensions.Configuration;
using YeelightAPI;
using YeelightAPI.Models;

namespace YeeMotion;

public class BulbService
{
    private readonly BulbStateTracker _bulbStateTracker;
    private readonly SunlightService _sunlightService;

    private readonly string _bulbAddress;
    private Device? _device;

    public BulbService(
        IConfiguration config,
        BulbStateTracker bulbStateTracker,
        SunlightService sunlightService)
    {
        _bulbStateTracker = bulbStateTracker;
        _sunlightService = sunlightService;
        _bulbAddress = config[Config.BulbAddress]!;
        bulbStateTracker.OnBulbStateChange.Register(ToggleBulb);
    }

    private async Task<Device> GetOrConnectDevice()
    {
        if (_device is { IsConnected: true })
        {
            return _device;
        }

        var device = new Device(_bulbAddress);
        await device.Connect();
        device.OnNotificationReceived += (_, args) => HandleBulbNotification(args);
        _bulbStateTracker.UpdateBulbPower(device.Properties.TryGetValue("power", out var power) && (string) power == "on");
        _device = device;
        return device;
    }

    public async Task ToggleBulb(bool power)
    {
        if (power == _bulbStateTracker.BulbPower)
        {
            return;
        }

        if (power && await _sunlightService.IsDaylight())
        {
            Console.WriteLine("Skipping, currently daylight");
            return;
        }

        Console.WriteLine($"Toggling bulb to {(power ? "On" : "Off")}");
        try
        {
            var bulb = await GetOrConnectDevice();
            await bulb.SetPower(power);
        }
        catch (Exception exc)
        {
            Console.WriteLine($"Failed to toggle bulb to {(power ? "On" : "Off")}: {exc.Message}");
            _device = null;
        }
    }

    private void HandleBulbNotification(NotificationReceivedEventArgs args)
    {
        var props = args.Result.Params;

        var powerState = (string) props[PROPERTIES.power] == "on";

        _bulbStateTracker.UpdateBulbPower(powerState);
    }
}