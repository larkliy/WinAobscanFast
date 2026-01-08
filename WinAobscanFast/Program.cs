using WinAobscanFast;
using System.Diagnostics;
using WinAobscanFast.Utils;
using WinAobscanFast.Enums;

var processId = ProcessUtils.FindByExeName("HD-Player.exe");
using var processHandle = ProcessUtils.OpenProcessById(processId);

var aob = new AobScan(processHandle);

const int runs = 10;

long totalMs = 0;
long totalTicks = 0;
int found = 0;

aob.Scan("20 20");

List<nint> list = null!;

for (int i = 0; i < runs; i++)
{
    var sw = Stopwatch.StartNew();

    list = aob.Scan("20 20");

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