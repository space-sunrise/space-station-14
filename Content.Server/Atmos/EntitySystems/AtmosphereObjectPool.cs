using System.Collections.Concurrent;

namespace Content.Server.Atmos.EntitySystems;

public sealed class AtmosphereObjectPool<T>
{
    private readonly ConcurrentBag<T> _objects;
    private readonly Func<T> _factory;
    private readonly Func<T, T> _reset;
    private readonly int _maxSize;

    public AtmosphereObjectPool(Func<T> factory, Func<T, T> reset, int maxSize = 1000)
    {
        _objects = new ConcurrentBag<T>();
        _factory = factory;
        _reset = reset;
        _maxSize = maxSize;
    }

    public T Get()
    {
        if (_objects.TryTake(out var item))
        {
            return item;
        }

        return _factory();
    }

    public void Return(T item)
    {
        if (_objects.Count >= _maxSize)
            return;

        var resetItem = _reset(item);
        _objects.Add(resetItem);
    }

    public void Clear()
    {
        while (_objects.TryTake(out _)) { }
    }
}
