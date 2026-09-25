# VirtualMemory

High-performance .NET virtual memory allocations

## Quick Start

`VirtualArena<T>` memory region:

```csharp
using var arena = new VirtualArena(1 << 30); // 1 GB

arena.EnsureCommitted(1024);

Console.WriteLine($"Arena.BaseAddress = {arena.BaseAddress:X}");
Console.WriteLine($"Arena.ReservedBytes = {arena.ReservedBytes}");
Console.WriteLine($"Arena.CommittedBytes = {arena.CommittedBytes}");
```

`VirtualList<T>` data structure:

```csharp
using var list = new VirtualList<int>(maxCapacity: 1_000_000);

for (var i = 0; i < 1_000_000; i += 1)
{
    list.Add(Random.Shared.Next());
}

var span = list.AsSpan();
foreach (var item in span)
{
    Console.WriteLine(item);
}
```
