using System.Diagnostics;
using TpacTool.IO;
using TpacTool.Lib;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    PrintUsage();
    return args.Length == 0 ? 2 : 0;
}

var tpacPath = args[0];
if (!File.Exists(tpacPath))
{
    Console.Error.WriteLine($"tpac file not found: {tpacPath}");
    return 1;
}

var pkg = new AssetPackage(tpacPath, loadHeaderNow: true, loadDataNow: false);
var textures = pkg.Items.OfType<Texture>().ToList();

if (args.Length >= 2 && args[1] == "--list")
{
    foreach (var t in textures.OrderBy(t => t.Name, StringComparer.Ordinal))
    {
        Console.WriteLine($"{t.Name}\t{t.Width}x{t.Height}\t{t.Format}");
    }
    return 0;
}

if (args.Length < 3)
{
    PrintUsage();
    return 2;
}

var spriteName = args[1];
var outDir = args[2];
var keepDds = args.Contains("--keep-dds");

var texture = textures.FirstOrDefault(t => t.Name == spriteName);
if (texture is null)
{
    Console.Error.WriteLine($"texture '{spriteName}' not found in {tpacPath}");
    Console.Error.WriteLine($"({textures.Count} textures in package; run with --list to see them)");
    return 1;
}

if (!texture.HasPixelData)
{
    Console.Error.WriteLine($"texture {spriteName} has no pixel data (header-only entry)");
    return 1;
}

Directory.CreateDirectory(outDir);
var ddsPath = Path.Combine(outDir, spriteName + ".dds");
var pngPath = Path.Combine(outDir, spriteName + ".png");

// Always go through DDS: TpacTool's PngExporter only handles uncompressed +
// DXT1/5 pixel layouts, and the common Bannerlord UI atlases are BC7 which
// it can't block-decode. DDS keeps the compressed payload intact; magick
// decodes BC7 natively.
TextureExporter.ExportToFile<DdsExporter>(ddsPath, texture, TextureExporter.TextureExportOption.NoMipmap);

var written = Directory.GetFiles(outDir, spriteName + "*.dds")
    .Where(p => File.GetLastWriteTimeUtc(p) > DateTime.UtcNow.AddSeconds(-5))
    .ToArray();
if (written.Length == 0)
{
    Console.Error.WriteLine("DDS export silently produced no files");
    return 1;
}

foreach (var dds in written)
{
    var png = Path.ChangeExtension(dds, ".png");
    var exit = RunMagick(dds, png);
    if (exit != 0)
    {
        Console.Error.WriteLine($"magick convert failed for {dds} (exit {exit})");
        return 1;
    }
    if (!keepDds) File.Delete(dds);
    Console.WriteLine(png);
}

return 0;

static int RunMagick(string input, string output)
{
    var psi = new ProcessStartInfo("magick", $"\"{input}\" \"{output}\"")
    {
        RedirectStandardError = true,
        RedirectStandardOutput = true,
        UseShellExecute = false,
    };
    try
    {
        using var p = Process.Start(psi)!;
        p.WaitForExit();
        if (p.ExitCode != 0)
        {
            Console.Error.Write(p.StandardError.ReadToEnd());
            Console.Error.Write(p.StandardOutput.ReadToEnd());
        }
        return p.ExitCode;
    }
    catch (System.ComponentModel.Win32Exception)
    {
        Console.Error.WriteLine("`magick` not on PATH; install ImageMagick 7+ (`dnf install ImageMagick`).");
        return 127;
    }
}

static void PrintUsage()
{
    Console.Error.WriteLine("usage:");
    Console.Error.WriteLine("  tpac-extract <tpac-file> --list");
    Console.Error.WriteLine("  tpac-extract <tpac-file> <texture-name> <output-dir> [--keep-dds]");
}
