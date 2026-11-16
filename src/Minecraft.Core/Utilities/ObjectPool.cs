namespace Minecraft.Core.Utilities;

public sealed class ObjectPool<T>
    where T : class, new()
{
    private readonly Stack<T> _available;
    private readonly Dictionary<T, long> _lended;
    private long _nextLease;
    private readonly Lock _lock = new();

    public ObjectPool(int numberOfObjects)
    {
        _available = new Stack<T>(numberOfObjects);
        _lended = new Dictionary<T, long>(numberOfObjects);

        for (int i = 0; i < numberOfObjects; i++)
        {
            _available.Push(new T());
        }
    }

    public T GetObject()
    {
        lock (_lock)
        {
            if (_available.Count <= 0)
            {
                _available.Push(new T());
            }

            T item = _available.Pop();
            _lended.Add(item, ++_nextLease);
            return item;
        }
    }

    public bool IsLentOut(T item)
    {
        lock (_lock)
        {
            return _lended.ContainsKey(item);
        }
    }

    public long LeaseOf(T item)
    {
        lock (_lock)
        {
            return _lended.TryGetValue(item, out long lease) ? lease : 0;
        }
    }

    public void ReturnObject(T item)
    {
        lock (_lock)
        {
            if (!_lended.Remove(item))
            {
                throw new InvalidOperationException("Trying to return an item that was not lended out.");
            }

            _available.Push(item);
        }
    }
}
