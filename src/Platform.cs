using VirtualMemory.Platforms;

namespace VirtualMemory;

public static class Platform
{
#if PLATFORM_WINDOWS
    private static readonly IPlatform _platform = new WindowsPlatform();
#elif PLATFORM_LINUX
    private static readonly IPlatform _platform = new LinuxPlatform();
#elif PLATFORM_MACOS
    private static readonly IPlatform _platform = new MacPlatform();
#else
#error Unsupported platform
#endif

    public static nint Reserve(nuint size) => _platform.Reserve(size);

    public static void Commit(nint address, nuint size) => _platform.Commit(address, size);

    public static void Decommit(nint address, nuint size) => _platform.Decommit(address, size);

    public static void Release(nint address, nuint size) => _platform.Release(address, size);
}
