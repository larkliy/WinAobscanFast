<div align="center">

  <img src="https://img.icons8.com/dusk/128/memory-slot.png" alt="logo" width="100" height="auto" />
  
  <h1>⚡ WinAobscanFast</h1>
  
  <p>
    <b>Blazing fast memory scanning (AOB) powered by SIMD & Parallelism.</b>
    <br>
    Written in modern C# for those who care about performance.
  </p>

  <!-- Badges -->
  <a href="#">
    <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet" alt=".NET Version" />
  </a>
  <a href="#">
    <img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=flat-square&logo=windows" alt="Platform" />
  </a>
  <a href="#">
    <img src="https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square" alt="License" />
  </a>
  <a href="#">
    <img src="https://img.shields.io/badge/SIMD-AVX%20%7C%20AVX512-red?style=flat-square" alt="SIMD" />
  </a>

</div>

<br>

## 🚀 Why this project?

Need to find a byte pattern in another process, but standard solutions are too slow or outdated? **WinAobscanFast** leverages modern hardware to get the job done instantly.

*   💎 **Hardware Intrinsics:** Built with `Vector512`, `Vector256`, and `Vector128`. If your CPU supports AVX-512, this scanner **flies**.
*   🧵 **Parallel Processing:** Memory is chunked and scanned concurrently across all available CPU threads.
*   🧠 **Smart Memory Mapping:** Automatically maps regions via `VirtualQueryEx`, skips `PAGE_GUARD`/`NOACCESS`, and merges adjacent regions to minimize syscalls.
*   🩸 **Modern C#:** Uses `Span<T>`, `LibraryImport`, `ArrayPool`, and zero-allocation techniques where possible.

---

## 📦 Installation

Just clone the repository and drop the project into your solution.

```bash
git clone https://github.com/larkliy/WinAobscanFast.git
```

## 🔥 Usage

Designed to be simple. No complex configuration, just raw speed.

### 1. Simple Pattern Scan

```csharp
using WinAobscanFast.Core;
using WinAobscanFast.Core.Implementations;

var results = AobScan.ScanProcess("Game Process.exe", "11 11 22 ?? ?? 22");

Console.WriteLine($"Found {results.Count} occurrences.");
foreach (var addr in results)
{
    Console.WriteLine($"Address: 0x{addr:X}");
}
```

### 2. Advanced Options

Need to scan only **Executable** memory (e.g., finding functions) or limit the address range?

```csharp
var options = new AobScanOptions
{
    // Filter regions: Only scan Executable + Readable memory
    MemoryAccess = MemoryAccess.Executable | MemoryAccess.Readable,
    
    // Optional: Restrict scan range
    MinScanAddress = 0x7FF00000000,
    MaxScanAddress = 0x7FFFFFFFFFF
};

var results = AobScan.ScanProcess("Game Process.exe", "11 11 22 ?? ?? 22", options);
```

---

## 🛠 Under the Hood

How do we achieve this performance?

1.  **Memory Mapping:** We don't read the entire RAM blindly. We query the memory map (`VirtualQueryEx`) to identify valid committed pages.
2.  **Chunking:** Valid regions are sliced into optimized chunks (default 256KB) for parallel processing.
3.  **SIMD:** The `IsMatch` method uses specific hardware instructions:
    ```csharp
    // Simplified logic
    if (Vector512.IsHardwareAccelerated) {
        // Compare 64 bytes in a single CPU cycle!
    }
    ```

---

## 📊 Performance

*On a modern CPU (e.g., i3-10100F), scanning 5.6GB of process memory typically takes 700-800 milliseconds, depending on pattern complexity and hit count.*

---

## 🤝 Contributing

Found a way to make it even faster? Found a bug?
**Pull Requests** and **Issues** are welcome!

1.  Fork it!
2.  Create your feature branch: `git checkout -b my-new-feature`
3.  Commit your changes: `git commit -am 'Add some feature'`
4.  Push to the branch: `git push origin my-new-feature`
5.  Submit a pull request

## ⭐ Support

If you found this project useful or interesting, please **give it a Star**! 🌟
It helps others find the project and motivates me to improve it.

---
<div align="center">
  <i>Made with ❤️ and C#</i>
</div>
