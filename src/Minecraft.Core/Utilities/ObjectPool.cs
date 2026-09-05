namespace Minecraft.Core.Utilities;

public sealed class ObjectPool<T>
    where T : class, new()
{
    private readonly Stack<T> _available;
    private readonly HashSet<T> _lended;
    private readonly Lock _lock = new();

    public ObjectPool(int numberOfObjects)
    {
        _available = new Stack<T>(numberOfObjects);
        _lended = new HashSet<T>(numberOfObjects);

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
            _lended.Add(item);
            return item;
        }
    }

    public bool IsLentOut(T item)
    {
        lock (_lock)
        {
            return _lended.Contains(item);
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
