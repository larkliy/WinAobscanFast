using AobscanFast.Structs.Unix;
using System.Runtime.InteropServices;

namespace AobscanFast.Utils;

internal partial class Native
{
    [LibraryImport("libc")]
    public static partial nint pread(int fd, Span<byte> buffer, nuint count, long offset);

    [LibraryImport("libc", SetLastError = true)]
    public static unsafe partial nint process_vm_readv(int pid, Iovec* local_iov, nuint liovcnt, Iovec* remote_iov, nuint riovcnt, nuint flags);
}
