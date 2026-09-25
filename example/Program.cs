using VirtualMemory;

using var arena = new VirtualArena(1 << 30);

Console.WriteLine($"Arena.BaseAddress = {arena.BaseAddress:X}");
Console.WriteLine($"Arena.ReservedBytes = {FormatBytes(arena.ReservedBytes)}");
Console.WriteLine($"Arena.CommittedBytes = {FormatBytes(arena.CommittedBytes)}");

return;

static string FormatBytes(nuint bytes)
{
    var suffixes = new string[] { "B", "KB", "MB", "GB", "TB" };
    var order = 0;
    double size = bytes;

    while (size >= 1024 && order < suffixes.Length - 1)
    {
        order += 1;
        size /= 1024;
    }

    return $"{size:0.##} {suffixes[order]}";
}
