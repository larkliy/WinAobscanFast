using WinAobscanFast.Core;

Console.OutputEncoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

var results = AobScan.ScanProcess("Godot_v4.6-stable_mono_win64.exe", "11 11 22 ?? ?? 22");

Console.WriteLine($"Результатов: {results.Count}");

foreach (nint result in results.Take(10))
{
    Console.WriteLine($"Address: 0x{result:X2}");
}

Console.WriteLine();