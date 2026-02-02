using Microsoft.Win32.SafeHandles;
using AobscanFast.Core.Abstractions;
using AobscanFast.Core.Implementations;

namespace AobscanFast.Core;

public static class ProcessMemoryFactory
{
    public static SafeProcessHandle OpenProcessByName(string processName)
    {
        if (OperatingSystem.IsWindows())
        {
            return OpenProcessByNameWindows(processName);
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return OpenProcessByNameUnix(processName);
        }
        else
        {
            throw new PlatformNotSupportedException($"Process memory operations are not supported on {Environment.OSVersion.Platform}.");
        }
    }

    public static IMemoryReader CreateMemoryReader(SafeProcessHandle processHandle)
    {
        if (OperatingSystem.IsWindows())
        {
            return CreateMemoryReaderWindows(processHandle);
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return CreateMemoryReaderUnix(processHandle);
        }
        else
        {
            throw new PlatformNotSupportedException($"Memory reading is not supported on {Environment.OSVersion.Platform}.");
        }
    }

    private static SafeProcessHandle OpenProcessByNameWindows(string processName)
    {
        var processId = WindowsProcessUtils.FindByName(processName);
        if (processId == 0)
        {
            throw new InvalidOperationException($"Process '{processName}' was not found.");
        }

        return WindowsProcessUtils.OpenProcess(processId);
    }

    private static SafeProcessHandle OpenProcessByNameUnix(string processName)
    {
        // TODO: Implement Unix-specific process opening
        throw new NotImplementedException("Unix process operations are not yet implemented.");
    }

    private static IMemoryReader CreateMemoryReaderWindows(SafeProcessHandle processHandle)
    {
        return new WindowsMemoryReader(processHandle);
    }

    private static IMemoryReader CreateMemoryReaderUnix(SafeProcessHandle processHandle)
    {
        // TODO: Implement Unix memory reader
        throw new NotImplementedException("Unix memory reading is not yet implemented.");
    }
}