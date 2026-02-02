using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using WinAobscanFast.Enums;
using WinAobscanFast.Structs;

namespace WinAobscanFast.Core.Implementations;

internal class WindowsProcessUtils
{
    public static SafeProcessHandle OpenProcess(uint processId)
        => Native.OpenProcess(ProcessAccessFlags.PROCESS_ALL_ACCESS, false, processId);

    [SkipLocalsInit]
    public static uint FindByName(ReadOnlySpan<char> name)
    {
        name = TrimExeExtension(name);

        var pe32 = new PROCESSENTRY32W { dwSize = (uint)Unsafe.SizeOf<PROCESSENTRY32W>() };

        using var snapshot = Native.CreateToolhelp32Snapshot(CreateToolhelpSnapshotFlags.TH32CS_SNAPPROCESS, 0);

        if (snapshot.IsInvalid)
            return 0;

        if (!Native.Process32FirstW(snapshot, ref pe32)) 
            return 0;

        do
        {
            ReadOnlySpan<char> currentFullname = pe32.szExeFile;

            int nullIndex = currentFullname.IndexOf('\0');

            ReadOnlySpan<char> currentName = nullIndex >= 0 ? currentFullname[..nullIndex] : currentFullname;

            currentName = TrimExeExtension(currentName);

            if (currentName.Equals(name, StringComparison.OrdinalIgnoreCase))
                return pe32.th32ProcessID;

        } while (Native.Process32NextW(snapshot, ref pe32));

        return 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ReadOnlySpan<char> TrimExeExtension(ReadOnlySpan<char> s)
            => s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? s[..^4] : s;
    }
}
