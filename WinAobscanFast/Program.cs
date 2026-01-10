using System.Diagnostics;
using WinAobscanFast.Core;
using WinAobscanFast.Core.Implementations;

var processId = WindowsProcessUtils.FindByName("HD-Player.exe");
using var handle = WindowsProcessUtils.OpenProcess(processId);

var aobscan = new AobScan(new WindowsMemoryReader(handle));

const int runs = 20;

long totalMs = 0;
long totalTicks = 0;
int found = 0;

aobscan.Scan("20 20");

List<nint> list = null!;

for (int i = 0; i < runs; i++)
{
    var sw = Stopwatch.StartNew();

    list = aobscan.Scan("20 20");

    sw.Stop();

    found = list.Count;
    totalMs += sw.ElapsedMilliseconds;
    totalTicks += sw.ElapsedTicks;
}

foreach (var item in list.Take(10))
{
    Console.WriteLine(item.ToString("X2"));
}

Console.WriteLine("--------------------------------Pool--------------------------------");
Console.WriteLine($"Прогонов:           {runs}");
Console.WriteLine($"Найдено вхождений:  {found}");
Console.WriteLine($"Среднее время:     {totalMs / (double)runs:F2} мс");
Console.WriteLine($"Средние тики:      {totalTicks / (double)runs:N0}");
Console.WriteLine("--------------------------------------------------------------------");
