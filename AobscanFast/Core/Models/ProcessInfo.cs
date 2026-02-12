using AobscanFast.Utils;

namespace AobscanFast.Core.Models;

internal struct ProcessInfo(nint processHandle) : IDisposable
{
    public nint ProcessId { get; set; } = processHandle;

    public readonly void Dispose()
    {
        if (OperatingSystem.IsWindows())
            Native.CloseHandle(ProcessId);
    }
}
