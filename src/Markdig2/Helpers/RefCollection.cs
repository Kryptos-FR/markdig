using System.Buffers;
using System.Runtime.CompilerServices;

namespace Markdig2.Helpers;

/// <summary>
/// A stack-allocated mutable buffer that uses ArrayPool for larger buffers.
/// This is a ref struct, so it can only be used on the stack.
/// </summary>
public ref struct RefCollection<T>
{
    private T[]? _arrayToReturnToPool;
    private Span<T> _buffer;
    private int _pos;

    /// <summary>
    /// Initializes a new instance using the provided buffer.
    /// </summary>
    /// <param name="initialBuffer">The initial buffer to use.</param>
    public RefCollection(Span<T> initialBuffer)
    {
        _arrayToReturnToPool = null;
        _buffer = initialBuffer;
        _pos = 0;
    }

    /// <summary>
    /// Initializes a new instance with the specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The initial capacity.</param>
    public RefCollection(int initialCapacity)
    {
        _arrayToReturnToPool = ArrayPool<T>.Shared.Rent(initialCapacity);
        _buffer = _arrayToReturnToPool;
        _pos = 0;
    }

    /// <summary>
    /// Gets the capacity of the buffer.
    /// </summary>
    public readonly int Capacity => _buffer.Length;

    /// <summary>
    /// Gets or sets the length of the buffer.
    /// </summary>
    public int Length
    {
        readonly get => _pos;
        set
        {
            if (value < 0 || value > _buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(value));
            _pos = value;
        }
    }

    /// <summary>
    /// Adds an item to the buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        int pos = _pos;
        Span<T> buffer = _buffer;
        if ((uint)pos < (uint)buffer.Length)
        {
            buffer[pos] = item;
            _pos = pos + 1;
        }
        else
        {
            GrowAndAdd(item);
        }
    }

    /// <summary>
    /// Gets a span representing the current contents.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<T> AsReadOnlySpan() => _buffer[.._pos];

    /// <summary>
    /// Gets a span representing the current contents (mutable).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> AsSpan() => _buffer;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowAndAdd(T item)
    {
        Grow(1);
        Add(item);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additionalCapacityRequired)
    {
        const int ArrayMaxLength = 0x7FFFFFC7;
        int newCapacity = Math.Max(_pos + additionalCapacityRequired, _buffer.Length * 2);

        if ((uint)newCapacity > ArrayMaxLength)
        {
            newCapacity = Math.Max(_pos + additionalCapacityRequired, ArrayMaxLength);
        }

        T[] poolArray = ArrayPool<T>.Shared.Rent(newCapacity);
        _buffer[.._pos].CopyTo(poolArray);

        T[]? toReturn = _arrayToReturnToPool;
        _buffer = _arrayToReturnToPool = poolArray;

        if (toReturn != null)
        {
            ArrayPool<T>.Shared.Return(toReturn);
        }
    }
}
