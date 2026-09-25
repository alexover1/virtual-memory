using System.Diagnostics;
using VirtualMemory;

const uint zoneCount = 100_000_000u;

using var events = new VirtualList<ZoneEvent>(maxCapacity: zoneCount);

while (true)
{
    var stopwatch = Stopwatch.StartNew();

    for (uint zone = 0; zone < zoneCount; zone += 1)
    {
        var start = Stopwatch.GetTimestamp();

        Thread.SpinWait(1);

        var end = Stopwatch.GetTimestamp();
        var duration = checked((ulong)(end - start));

        if (duration > uint.MaxValue)
        {
            throw new InvalidOperationException("Duration exceeded maximum for 32-bit unsigned integer.");
        }

        events.Add(new ZoneEvent(start, (uint)duration, zone));
    }

    stopwatch.Stop();

    Console.WriteLine($"Events.Count = {events.Count}");
    Console.WriteLine($"Events.Capacity = {events.Capacity}");
    Console.WriteLine($"Events.MaxCapacity = {events.MaxCapacity}");
    Console.WriteLine($"Events.CommittedBytes = {FormatBytes(events.CommittedBytes)}");
    Console.WriteLine($"Events.ReservedBytes = {FormatBytes(events.ReservedBytes)}");
    Console.WriteLine($"Total Time = {stopwatch.Elapsed}");

    events.Clear();
}

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

public readonly struct ZoneEvent
{
    public ZoneEvent(long startTimestamp, uint elapsedTicks, uint zoneIndex)
    {
        StartTimestamp = startTimestamp;
        ElapsedTicks = elapsedTicks;
        ZoneIndex = zoneIndex;
    }

    public long StartTimestamp { get; }
    public uint ElapsedTicks { get; }
    public uint ZoneIndex { get; }
}
