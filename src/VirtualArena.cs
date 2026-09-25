namespace VirtualMemory;

public class VirtualArena : IDisposable
{
    public const uint DefaultCommitChunkSize = 1 << 20; // NOTE(alex): One megabyte!

    public nint BaseAddress { get; private set; }

    public nuint ReservedBytes { get; private set; }

    public nuint CommittedBytes { get; private set; }

    public nuint PageSize { get; }

    public nuint CommitChunkSize { get; }

    public VirtualArena(nuint reserveSize, nuint commitChunkSize = DefaultCommitChunkSize)
    {
        PageSize = checked((nuint)Environment.SystemPageSize);
        ReservedBytes = AlignUp(reserveSize, PageSize);

        if (ReservedBytes > (nuint)nint.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(reserveSize), "The requested size is larger than the process can address contiguously.");
        }

        CommitChunkSize = AlignUp(CommitChunkSize, PageSize);
        BaseAddress = Platform.Reserve(ReservedBytes);
    }

    public void EnsureAccessible(nuint requiredByteCount)
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

        var target = RoundUpClamped(requiredByteCount, CommitChunkSize, ReservedBytes);
        var bytesToCommit = target - CommittedBytes;
        var commitAddress = BaseAddress + checked((nint)CommittedBytes);

        Platform.Commit(commitAddress, bytesToCommit);
        CommittedBytes = target;
    }

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
