using System.Device.Gpio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YeeMotion;

var config = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>()
    {
        [Config.BulbAddress] = "192.168.178.38",
    })
    .Build();


var services = new ServiceCollection()
    .AddSingleton<BulbService>()
    .AddSingleton<BulbStateTracker>()
    .AddSingleton<IConfiguration>(config)
    .BuildServiceProvider();

var pin = int.Parse(args[0]);

using var gpioController = new GpioController(PinNumberingScheme.Logical);

gpioController.OpenPin(pin, PinMode.InputPullDown);

var bulbStateTracker = services.GetRequiredService<BulbStateTracker>();
_ = services.GetRequiredService<BulbService>();

try
{
    gpioController.RegisterCallbackForPinValueChangedEvent(pin, PinEventTypes.Rising | PinEventTypes.Falling, async (_, eArgs) =>
    {
        await bulbStateTracker.NotifyMovement();
    });
    
    await Task.Delay(-1);
}
finally
{
    gpioController.ClosePin(pin);
}