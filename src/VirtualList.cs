using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VirtualMemory;

public class VirtualList<T> : IDisposable where T : unmanaged
{
    private static readonly nuint ElementSize = checked((nuint)Marshal.SizeOf<T>()); // NOTE(alex): This really bothers me that you can't use regular sizeof(T) without unsafe _and_ it's not even a compile-time constant...

    private unsafe T* _data;

    public nuint Count { get; private set; }

    public nuint Capacity { get; private set; }

    public nuint MaxCapacity { get; private set; }

    public nuint CommittedBytes => Arena.CommittedBytes;

    public nuint ReservedBytes => Arena.ReservedBytes;

    public VirtualArena Arena { get; }

    public VirtualList(nuint maxCapacity, nuint commitChunkBytes = VirtualArena.DefaultCommitChunkBytes)
    {
        var reserveBytes = checked(maxCapacity * ElementSize);

        Arena = new VirtualArena(reserveBytes, commitChunkBytes);
        MaxCapacity = maxCapacity;
        Capacity = CapacityFromCommittedBytes(Arena.CommittedBytes);

        unsafe
        {
            _data = (T *)Arena.BaseAddress;
        }
    }

    public ref T this[nuint index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            unsafe
            {
                return ref *(_data + (nint)index);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        var index = Count;
        if (index >= Capacity)
        {
            EnsureCapacity(index + 1);
        }

        unsafe
        {
            *(_data + (nint)index) = item;
        }
        Count = index + 1;
    }

    public void AddRange(ReadOnlySpan<T> items)
    {
        if (items.IsEmpty)
        {
            return;
        }

        var oldCount = Count;
        var newCount = checked(oldCount + (nuint)items.Length);
        EnsureCapacity(newCount);

        unsafe
        {
            var destination = new Span<T>(_data + (nint)oldCount, items.Length);
            items.CopyTo(destination);
        }

        Count = newCount;
    }

    public void EnsureCapacity(nuint requiredCapacity)
    {
        if (requiredCapacity <= Capacity)
        {
            return;
        }

        if (requiredCapacity > MaxCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredCapacity));
        }

        var arena = Arena;
        var requiredBytes = checked(requiredCapacity * ElementSize);
        arena.EnsureCommitted(requiredBytes);
        Capacity = CapacityFromCommittedBytes(arena.CommittedBytes);
    }

    public void Clear(bool decommitPages = false)
    {
        Count = 0;

        if (!decommitPages)
        {
            return;
        }

        var arena = Arena;
        arena.DecommitAll();
        Capacity = 0;
    }

    public Span<T> AsSpan()
    {
        if (Count > int.MaxValue)
        {
            throw new InvalidOperationException("The list is too large for a single Span<T>. Use GetSpan windows instead.");
        }

        unsafe
        {
            return new Span<T>(_data, (int)Count);
        }
    }

    public Span<T> GetSpan(nuint start, int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        var unsignedLength = (nuint)length;
        if (start > Count || unsignedLength > Count - start)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        unsafe
        {
            return new Span<T>(_data + (nint)start, length);
        }
    }

    public void Dispose()
    {
        unsafe
        {
            if (_data is null)
            {
                return;
            }

            _data = null;
        }

        Arena.Dispose();
        Count = 0;
        Capacity = 0;
        MaxCapacity = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private nuint CapacityFromCommittedBytes(nuint committedBytes)
    {
        var capacity = CommittedBytes / ElementSize;
        return capacity < MaxCapacity ? capacity : MaxCapacity;
    }
}
