using System.Collections.Concurrent;

namespace YeeMotion;

public class AsyncEvent<T>
{
    private readonly ConcurrentBag<Func<T, Task>> _handlers = new();
    
    public async Task Publish(T args)
    {
        var handlers = _handlers.ToList();

        await Task.WhenAll(handlers.Select(x => x(args)));
    }

    public void Register(Func<T, Task> handler)
    {
        _handlers.Add(handler);
    }
}