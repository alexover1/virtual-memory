namespace VirtualMemory;

public class VirtualArena : IDisposable
{
    public const uint DefaultCommitChunkBytes = 1 << 20; // NOTE(alex): One megabyte!

    public nint BaseAddress { get; private set; }

    public nuint ReservedBytes { get; private set; }

    public nuint CommittedBytes { get; private set; }

    public nuint PageSize { get; }

    public nuint CommitChunkBytes { get; }

    public VirtualArena(nuint reserveBytes, nuint commitChunkBytes = DefaultCommitChunkBytes)
    {
        PageSize = checked((nuint)Environment.SystemPageSize);
        ReservedBytes = AlignUp(reserveBytes, PageSize);

        if (ReservedBytes > (nuint)nint.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(reserveBytes), "The requested size is larger than the process can address contiguously.");
        }

        CommitChunkBytes = AlignUp(commitChunkBytes, PageSize);
        BaseAddress = Platform.Reserve(ReservedBytes);
    }

    public void EnsureCommitted(nuint requiredByteCount)
    {
        if (requiredByteCount <= CommittedBytes)
        {
            // NOTE(alex): We already have committed enough space to hold the allocation.
            return;
        }

        if (requiredByteCount > ReservedBytes)
        {
            // NOTE(alex): This is where you might want to chain arenas in order to support growing dynamically.
            // For now, we just say that if you run out of virtual address space, you can't allocate any more
            // memory using this arena, since you might not be able to guarantee reserving more memory and
            // getting back an address space that is contiguous with what we already allocated.
            throw new ArgumentOutOfRangeException(nameof(requiredByteCount), "Ran out of reserved virtual address space.");
        }

        var target = RoundUpClamped(requiredByteCount, CommitChunkBytes, ReservedBytes);
        var bytesToCommit = target - CommittedBytes;
        var commitAddress = BaseAddress + checked((nint)CommittedBytes);

        Platform.Commit(commitAddress, bytesToCommit);
        CommittedBytes = target;
    }

    public void DecommitAfter(nuint byteCountToKeep)
    {
        if (byteCountToKeep > ReservedBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(byteCountToKeep));
        }

        var keepCommitted = Math.Min(AlignUp(byteCountToKeep, PageSize), CommittedBytes);

        var bytesToDecommit = CommittedBytes - keepCommitted;
        if (bytesToDecommit == 0)
        {
            return;
        }

        var decommitAddress = BaseAddress + checked((nint)keepCommitted);
        Platform.Decommit(decommitAddress, bytesToDecommit);
        CommittedBytes = keepCommitted;
    }

    public void DecommitAll() => DecommitAfter(0);

    public void Dispose()
    {
        // NOTE(alex): There is no flag indicated whether the arena has previously been disposed.
        // If we have memory still allocated, then when we get disposed we release it. That's it.
        if (BaseAddress == 0)
        {
            return;
        }

        Platform.Release(BaseAddress, ReservedBytes);

        BaseAddress = 0;
        ReservedBytes = 0;
        CommittedBytes = 0;
        GC.SuppressFinalize(this);
    }

    ~VirtualArena()
    {
        if (BaseAddress == 0)
        {
            return;
        }

        try
        {
            Platform.Release(BaseAddress, ReservedBytes);

            BaseAddress = 0;
            ReservedBytes = 0;
            CommittedBytes = 0;
        }
        catch
        {
            // NOTE(alex): Finalizers are not allowed to throw exceptions.
        }
    }

    private static nuint AlignUp(nuint value, nuint alignment)
    {
        var remainder = value % alignment;
        if (remainder == 0)
        {
            return value;
        }

        return checked(value + (alignment - remainder));
    }

    private static nuint RoundUpClamped(nuint value, nuint quantum, nuint maximum)
    {
        var remainder = value % quantum;
        if (remainder == 0)
        {
            return value;
        }

        var add = quantum - remainder;
        if (add > maximum - value)
        {
            return maximum;
        }

        return value + add;
    }
}
