using System.Threading;
using DequeNet;

namespace Yautbox.InMemory.Collections;

internal sealed class LimitedConcurrentDeque<T>(int capacity)
{
    private readonly ConcurrentDeque<T> _deque = [];
    private readonly SemaphoreSlim _semaphore = new(capacity);

    public bool TryPopLeft(out T obj)
    {
        var isPop = _deque.TryPopLeft(out obj);

        if (isPop)
            _semaphore.Release();

        return isPop;
    }

    public void PushLeft(T obj) => _deque.PushLeft(obj);

    public void PushRight(T obj)
    {
        _semaphore.Wait();
        _deque.PushRight(obj);
    }
}
