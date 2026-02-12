using System.Runtime.CompilerServices;

namespace AobscanFast.Core.Models.Pattern;

internal abstract class PatternBase
{
    public abstract byte[] Bytes { get; protected set; }
    public abstract void ScanChunk(in MemoryRange mbi, List<nint> threadLocalList, in Span<byte> buffer);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool IsMatch(ref Span<byte> data) => Bytes.SequenceEqual(data);
}
