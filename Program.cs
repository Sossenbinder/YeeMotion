using System.Device.Gpio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YeelightAPI;
using YeeMotion;

// Support both command line args and environment variables
var pin = args.Length > 0
    ? int.Parse(args[0])
    : int.Parse(Environment.GetEnvironmentVariable("GPIO_PIN") ?? throw new InvalidOperationException("GPIO_PIN not set"));

var bulbAddress = args.Length > 1
    ? args[1]
    : Environment.GetEnvironmentVariable("BULB_ADDRESS") ?? throw new InvalidOperationException("BULB_ADDRESS not set");

var config = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>()
    {
        [Config.BulbAddress] = bulbAddress,
    })
    .Build();

var services = new ServiceCollection()
    .AddSingleton<BulbService>()
    .AddSingleton<BulbStateTracker>()
    .AddSingleton<IConfiguration>(config)
    .BuildServiceProvider();

using var gpioController = new GpioController(PinNumberingScheme.Logical);

gpioController.OpenPin(pin, PinMode.InputPullDown);

var bulbStateTracker = services.GetRequiredService<BulbStateTracker>();
_ = services.GetRequiredService<BulbService>();

Console.WriteLine($"YeeMotion started - GPIO pin: {pin}, Bulb: {bulbAddress}");

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
