namespace VirtualMemory;

public interface IPlatform
{
    public nint Reserve(nuint size);
    public void Commit(nint address, nuint size);
    public void Decommit(nint address, nuint size);
    public void Release(nint address, nuint size);
}
