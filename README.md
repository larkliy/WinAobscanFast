# WinAobscanFast

A fast **Array of Bytes (AoB) memory scanner** for Windows written in **C# (.NET)**.

This project scans another process memory for byte patterns with wildcard support, using low-level Windows APIs and performance-oriented techniques.

## Features

- 🚀 High-performance AoB scanning
- 🧵 Parallel memory region scanning
- 🧠 Optimized search using the longest solid byte sequence
- ❓ Wildcard support (`?` / `??`)
- 🧩 SIMD-accelerated pattern matching (`System.Numerics.Vector`)
- 🔍 Memory access filtering (Readable / Writable / Executable)

## Example

```csharp
var processId = ProcessUtils.FindByExeName("notepad.exe");
using var processHandle = ProcessUtils.OpenProcessById(processId);

var scanner = new AobScan(processHandle);

var results = scanner.Scan("48 8B ?? ?? ?? 89");
```

