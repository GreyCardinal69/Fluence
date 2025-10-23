namespace Fluence.VirtualMachine
{
    internal sealed class ObjectPool<T> where T : class, new()
    {
        private readonly Stack<T> _pool = new Stack<T>();
        private readonly Action<T>? _resetAction;

        internal ObjectPool(Action<T> resetAction = null!, int initialCapacity = 16)
        {
            _resetAction = resetAction;
            for (int i = 0; i < initialCapacity; i++)
            {
                _pool.Push(new T());
            }
        }

        /// <summary>
        /// Gets an object from the pool. If the pool is empty, a new object is created.
        /// </summary>
        internal T Get()
        {
            if (_pool.TryPop(out T? item))
            {
                return item;
            }
            return new T();
        }

        /// <summary>
        /// Returns an object to the pool for reuse.
        /// </summary>
        internal void Return(T item)
        {
            _resetAction?.Invoke(item);
            _pool.Push(item);
        }
    }
}