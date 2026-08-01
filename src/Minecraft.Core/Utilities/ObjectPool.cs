namespace Minecraft.Core.Utilities;

/// <summary>
/// A thread safe pool of reusable instances. Chunks are large enough that allocating a fresh one per load
/// would keep the garbage collector busy, so they are recycled through here instead.
/// </summary>
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
