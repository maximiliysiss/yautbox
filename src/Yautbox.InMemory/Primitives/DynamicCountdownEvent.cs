using System.Collections.Concurrent;

namespace Yautbox.InMemory.Primitives;

internal sealed class DynamicCountdownEvent<T> where T : notnull
{
    private readonly ConcurrentDictionary<T, byte> _elements = [];
    private readonly AutoResetEvent _zero = new(initialState: true);

    public void Wait() => _zero.WaitOne();

    public void Acquire(IEnumerable<T> elements)
    {
        foreach (var element in elements)
            _elements.TryAdd(element, 0);
    }

    public void Release(IEnumerable<T> elements)
    {
        foreach (var element in elements)
            _elements.TryRemove(element, out _);

        if (_elements.IsEmpty)
            _zero.Set();
    }
}
