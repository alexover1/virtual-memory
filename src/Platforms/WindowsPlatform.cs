using VirtualMemory;

namespace VirtualMemory.Platforms;

public class WindowsPlatform : IPlatform
{
    /// <inheritdoc/>
    public nint Reserve(nuint size) => throw new NotImplementedException();

    /// <inheritdoc/>
    public void Commit(nint address, nuint size) => throw new NotImplementedException();

    /// <inheritdoc/>
    public void Decommit(nint address, nuint size) => throw new NotImplementedException();

    /// <inheritdoc/>
    public void Release(nint address, nuint size) => throw new NotImplementedException();
}
