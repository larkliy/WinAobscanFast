using WinAobscanFast;
using System.Diagnostics;
using WinAobscanFast.Utils;
using WinAobscanFast.Enums;

var processId = ProcessUtils.FindByExeName("HD-Player.exe");
using var processHandle = ProcessUtils.OpenProcessById(processId);

var aob = new AobScan(processHandle);

Stopwatch sw = Stopwatch.StartNew();

var list = aob.Scan("20 20 10 55 ?? 92 93", MemoryAccess.Readable | MemoryAccess.Writable);

sw.Stop();

Console.WriteLine("--------------------------------Pool--------------------------------");
Console.WriteLine($"Найдено вхождений: {list.Count}");
Console.WriteLine($"Затрачено времени: {sw.ElapsedMilliseconds} мс");
Console.WriteLine($"Тиков процессора:  {sw.ElapsedTicks:N0}");
Console.WriteLine("--------------------------------------------------------------------");