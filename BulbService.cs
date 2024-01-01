using Microsoft.Extensions.Configuration;
using YeelightAPI;
using YeelightAPI.Models;

namespace YeeMotion;

public class BulbService
{
    private readonly BulbStateTracker _bulbStateTracker;

    private readonly Lazy<Task<Device>> _bulb;
    
    public BulbService(
        IConfiguration config,
        BulbStateTracker bulbStateTracker)
    {
        _bulbStateTracker = bulbStateTracker;
        bulbStateTracker.OnBulbStateChange.Register(ToggleBulb);
        
        _bulb = new Lazy<Task<Device>>(async () =>
        {
            var device = new Device(config[Config.BulbAddress]);

            await device.Connect();
 
            device.OnNotificationReceived += (_, args) => HandleBulbNotification(args);
            bulbStateTracker.UpdateBulbPower((string) device.Properties["power"] == "on");
            
            return device;
        });
    }

    public async Task ToggleBulb(bool power)
    {
        var bulb = await _bulb.Value;

        if (power == _bulbStateTracker.BulbPower)
        {
            return;
        }

        var time = DateTime.UtcNow;

        if (time.Hour is > 7 and < 16)
        {
            Console.WriteLine("Invalid time");
            return;
        }
        
        Console.WriteLine($"Toggling bulb to {(power ? "On" : "Off")}");
        try
        {
            await bulb.SetPower(power);
        }
        catch (Exception exc)
        {
            Console.WriteLine($"Failed to toggle bulb to {(power ? "On" : "Off")}");
        }
    }

    private void HandleBulbNotification(NotificationReceivedEventArgs args)
    {
        var props = args.Result.Params;

        var powerState = (string) props[PROPERTIES.power] == "on";

        _bulbStateTracker.UpdateBulbPower(powerState);
    }
}