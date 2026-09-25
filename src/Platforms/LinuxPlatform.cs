using System.ComponentModel;
using System.Runtime.InteropServices;
using VirtualMemory;

namespace VirtualMemory.Platforms;

public class LinuxPlatform : IPlatform
{
    private const int ProtNone = 0x0;
    private const int ProtRead = 0x1;
    private const int ProtWrite = 0x2;

    private const int MapPrivate = 0x2;
    private const int MapFixed = 0x10;
    private const int MapAnonymous = 0x20;

    private const nint MapFailed = (nint)(-1);

    /// <inheritdoc/>
    public nint Reserve(nuint size)
    {
        var mapping = Mmap(0, size, ProtNone, MapPrivate | MapAnonymous, -1, 0);
        if (mapping == MapFailed)
        {
            ThrowLastError("Failed to reserve virtual address space.");
        }

        return mapping;
    }

    /// <inheritdoc/>
    public void Commit(nint address, nuint size)
    {
        if (size != 0 && Mprotect(address, size, ProtRead | ProtWrite) != 0)
        {
            ThrowLastError("Failed to make reserved pages accessible.");
        }
    }

    /// <inheritdoc/>
    public void Decommit(nint address, nuint size)
    {
        if (size == 0)
        {
            return;
        }

        // NOTE(alex): Replace this page-aligned subrange with a fresh inaccessible anonymous
        // mapping. That discards the previous physical backing and restores the reserve-like
        // PROT_NONE state while preserving the address range (writes to this region will segfault).
        var mapping = Mmap(address, size, ProtNone, MapPrivate | MapFixed | MapAnonymous, -1, 0);
        if (mapping == MapFailed)
        {
            ThrowLastError("Failed to decommit pages.");
        }
    }

    /// <inheritdoc/>
    public void Release(nint address, nuint size)
    {
        if (address != 0 && Munmap(address, size) != 0)
        {
            ThrowLastError("Failed to release virtual address space.");
        }
    }

    private static void ThrowLastError(string message)
    {
        var error = Marshal.GetLastPInvokeError();
        throw new Win32Exception(error, message);
    }

    [DllImport("libc", EntryPoint = "mmap", SetLastError = true)]
    private static extern nint Mmap(nint address, nuint length, int protection, int flags, int fileDescriptor, nint offset);

    [DllImport("libc", EntryPoint = "mprotect", SetLastError = true)]
    private static extern nint Mprotect(nint address, nuint length, int protection);

    [DllImport("libc", EntryPoint = "munmap", SetLastError = true)]
    private static extern nint Munmap(nint address, nuint length);
}
