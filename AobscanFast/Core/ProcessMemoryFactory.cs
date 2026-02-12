using AobscanFast.Core.Implementations.Unix;
using AobscanFast.Core.Implementations.Windows;
using AobscanFast.Core.Models;

namespace AobscanFast.Core;

public static class ProcessMemoryFactory
{
    internal static ProcessInfo OpenProcessByName(string processName)
    {
        if (OperatingSystem.IsWindows())
        {
            var processId = WindowsProcessUtils.FindByName(processName);
            return WindowsProcessUtils.OpenProcess(processId);
        }
       
        throw new NotImplementedException();
    }

    internal static ProcessInfo OpenProcessById(uint processId)
    {
        if (OperatingSystem.IsWindows())
            return WindowsProcessUtils.OpenProcess(processId);

        throw new NotImplementedException();
    }

    internal static ModuleInfo GetModule(uint processId, string moduleName)
    {
        if (OperatingSystem.IsWindows())
        {
            var (@base, size) = WindowsProcessUtils.GetModule(processId, moduleName);
            return new(@base, size);
        }
        
        throw new NotImplementedException();
    }

    internal static uint FindProcessIdByName(string processName)
    {
        if (OperatingSystem.IsWindows())
            return WindowsProcessUtils.FindByName(processName);
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            return UnixProcessUtils.FindByName(processName);

        throw new NotImplementedException();
    }

    internal static IMemoryReader CreateMemoryReader(ProcessInfo processInfo)
    {
        if (OperatingSystem.IsWindows())
            return new WindowsMemoryReader(processInfo);
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            return new UnixMemoryReader(processInfo);

        throw new NotImplementedException();
    }
}