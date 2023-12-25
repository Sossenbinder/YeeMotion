namespace YeeMotion;

public class BulbStateTracker
{
    public AsyncEvent<bool> OnBulbStateChange { get; } = new();

    public bool BulbPower { get; private set; }
    
    private DateTime? _lastMovement;

    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);

    public BulbStateTracker()
    {
        _ = RunTimeoutTracker();
    }

    public async ValueTask NotifyMovement()
    {
        var tmpLastMovement = _lastMovement;
        _lastMovement = DateTime.UtcNow;

        if (tmpLastMovement == _lastMovement)
        {
            return;
        }
        
        // Movement changed, turn on
        Console.WriteLine($"{DateTime.UtcNow}: Movement detected, turning on");
        await OnBulbStateChange.Publish(true);
    }

    public void UpdateBulbPower(bool power)
    {
        Console.WriteLine($"Received bulb notification with power: {(power ? "On" : "Off")}");
        BulbPower = power;
    }

    private async Task RunTimeoutTracker()
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                // No movement at all
                if (!_lastMovement.HasValue)
                {
                    continue;
                }

                // Check if last movement occured, but not outside the timeout
                var currentTime = DateTime.UtcNow;
                if (currentTime - _lastMovement < _timeout)
                {
                    continue;
                }

                // Reset timeout and publish off state
                _lastMovement = null;
                await OnBulbStateChange.Publish(false);
                Console.WriteLine($"{DateTime.UtcNow}: Movement lost, turning off");
            }
            catch (Exception exc)
            {
                Console.WriteLine("Error occured in state tracking");
            }
        }
    }
}