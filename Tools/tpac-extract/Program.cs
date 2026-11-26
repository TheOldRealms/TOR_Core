using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using SharpGLTF.Scenes;
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

if (args.Length >= 2)
{
    switch (args[1])
    {
        case "--list":
            return ListTextures(pkg);
        case "--list-anims":
            return ListAnimations(pkg);
        case "--list-clips":
            return ListClips(pkg);
        case "--list-all":
            return ListAll(pkg);
        case "--dump-anim":
            if (args.Length < 4) { PrintUsage(); return 2; }
            return DumpAnimation(pkg, args[2], args[3]);
        case "--export-gltf":
            if (args.Length < 4) { PrintUsage(); return 2; }
            return ExportGltf(pkg, tpacPath, args[2], args[3],
                ExtractOption("--skeletons"),
                ExtractFpsOption(),
                ExtractOption("--skeleton-name"));
    }
}

string? ExtractOption(string name)
{
    for (var i = 4; i < args.Length - 1; i++)
        if (args[i] == name) return args[i + 1];
    return null;
}

float ExtractFpsOption()
{
    var raw = ExtractOption("--fps");
    return raw is null ? 30f : float.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
}

if (args.Length < 3)
{
    PrintUsage();
    return 2;
}

var spriteName = args[1];
var outDir = args[2];
var keepDds = args.Contains("--keep-dds");

var textures = pkg.Items.OfType<Texture>().ToList();
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

static int ListTextures(AssetPackage pkg)
{
    var textures = pkg.Items.OfType<Texture>().ToList();
    foreach (var t in textures.OrderBy(t => t.Name, StringComparer.Ordinal))
    {
        Console.WriteLine($"{t.Name}\t{t.Width}x{t.Height}\t{t.Format}");
    }
    return 0;
}

static int ListAnimations(AssetPackage pkg)
{
    var anims = pkg.Items.OfType<SkeletalAnimation>().ToList();
    foreach (var a in anims.OrderBy(a => a.Name, StringComparer.Ordinal))
    {
        var hasDef = a.Definition is not null ? "data" : "header-only";
        Console.WriteLine($"{a.Name}\tbones={a.BoneNum}\tduration={a.Duration}\tskeleton={a.Skeleton}\t{hasDef}");
    }
    return 0;
}

static int ListClips(AssetPackage pkg)
{
    var clips = pkg.Items.OfType<AnimationClip>().ToList();
    foreach (var c in clips.OrderBy(c => c.Name, StringComparer.Ordinal))
    {
        Console.WriteLine($"{c.Name}\tduration={c.Duration:F3}\tprio={c.Priority}\tanim={c.Animation}\tflags=[{string.Join(",", c.Flags)}]");
    }
    return 0;
}

static int ListAll(AssetPackage pkg)
{
    var groups = pkg.Items.GroupBy(i => i.GetType().Name).OrderBy(g => g.Key, StringComparer.Ordinal);
    foreach (var g in groups)
    {
        Console.WriteLine($"# {g.Key} ({g.Count()})");
        foreach (var it in g.OrderBy(i => i.Name, StringComparer.Ordinal))
            Console.WriteLine($"  {it.Name}\t{it.Guid}");
    }
    return 0;
}

static int DumpAnimation(AssetPackage pkg, string animName, string outDir)
{
    var anims = pkg.Items.OfType<SkeletalAnimation>().ToList();
    var anim = anims.FirstOrDefault(a => a.Name == animName);
    if (anim is null)
    {
        Console.Error.WriteLine($"animation '{animName}' not found in package");
        Console.Error.WriteLine($"({anims.Count} skeletal animations; run with --list-anims to see them)");
        return 1;
    }
    if (anim.Definition is null)
    {
        Console.Error.WriteLine($"animation {animName} has no definition data (header-only entry)");
        return 1;
    }

    Directory.CreateDirectory(outDir);
    var def = anim.Definition.Data;

    var payload = new
    {
        name = anim.Name,
        guid = anim.Guid.ToString(),
        skeletonGuid = anim.Skeleton.ToString(),
        geometryGuid = anim.GeometryGuid.ToString(),
        boneCount = anim.BoneNum,
        durationTicks = anim.Duration,
        rootPositionFrames = def.RootPositionFrames
            .Select(kv => new { t = kv.Key, x = kv.Value.Value.X, y = kv.Value.Value.Y, z = kv.Value.Value.Z, w = kv.Value.Value.W })
            .ToArray(),
        rootScaleFrames = def.RootScaleFrames
            .Select(kv => new { t = kv.Key, x = kv.Value.Value.X, y = kv.Value.Value.Y, z = kv.Value.Value.Z })
            .ToArray(),
        bones = def.BoneAnims.Select((b, i) => new
        {
            index = i,
            positionFrameCount = b.PositionFrames.Count,
            rotationFrameCount = b.RotationFrames.Count,
            hasPosition = b.HasPositionTransform(),
            hasRotation = b.HasRotationTransform(),
            positionFrames = b.PositionFrames
                .Select(kv => new { t = kv.Key, x = kv.Value.Value.X, y = kv.Value.Value.Y, z = kv.Value.Value.Z, w = kv.Value.Value.W })
                .ToArray(),
            rotationFrames = b.RotationFrames
                .Select(kv => new { t = kv.Key, x = kv.Value.Value.X, y = kv.Value.Value.Y, z = kv.Value.Value.Z, w = kv.Value.Value.W })
                .ToArray(),
        }).ToArray(),
    };

    var jsonPath = Path.Combine(outDir, animName + ".json");
    var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(jsonPath, json);
    Console.WriteLine(jsonPath);
    return 0;
}

static int ExportGltf(AssetPackage animPkg, string animTpacPath, string animName, string outPath,
    string? skeletonsPath, float fps, string? skeletonName)
{
    var anim = animPkg.Items.OfType<SkeletalAnimation>().FirstOrDefault(a => a.Name == animName);
    if (anim is null)
    {
        Console.Error.WriteLine($"animation '{animName}' not found in package");
        return 1;
    }
    if (anim.Definition is null)
    {
        Console.Error.WriteLine($"animation {animName} has no definition data (header-only entry)");
        return 1;
    }

    skeletonsPath ??= DefaultSkeletonsPath(animTpacPath);
    if (!File.Exists(skeletonsPath))
    {
        Console.Error.WriteLine($"skeletons tpac not found: {skeletonsPath}");
        Console.Error.WriteLine("pass --skeletons <path> to point at a different package");
        return 1;
    }

    var skelPkg = new AssetPackage(skeletonsPath, loadHeaderNow: true, loadDataNow: false);
    // SkeletalAnimation.Skeleton is a canonical engine-level ID (e.g. the shared "human" skeleton)
    // that doesn't map to any single Skeleton asset in skeletons.tpac. Multiple Skeleton assets
    // share the same bone topology, so we pick any match by bone count.
    var skeleton = ResolveSkeleton(skelPkg, anim.BoneNum, skeletonName);
    if (skeleton is null)
    {
        Console.Error.WriteLine($"no skeleton found with {anim.BoneNum} bones in {skeletonsPath}");
        if (skeletonName is not null)
            Console.Error.WriteLine($"(looked for name match: '{skeletonName}')");
        return 1;
    }
    Console.Error.WriteLine($"using skeleton '{skeleton.Name}' ({skeleton.Guid})");
    if (skeleton.Definition is null)
    {
        Console.Error.WriteLine($"skeleton {skeleton.Name} has no definition data");
        return 1;
    }

    var skelDef = skeleton.Definition.Data;
    var animDef = anim.Definition.Data;

    if (skelDef.Bones.Count != animDef.BoneAnims.Count)
    {
        Console.Error.WriteLine($"bone-count mismatch: skeleton has {skelDef.Bones.Count}, animation has {animDef.BoneAnims.Count}");
        return 1;
    }

    // armature anchor so RootPositionFrames can live on their own node
    // rather than overwriting bone[0]'s rest frame. Rotating -90° about X
    // converts Bannerlord's Z-up characters to glTF's Y-up convention so
    // Blender's default import rotation lands them upright.
    var armature = new NodeBuilder("armature");
    armature.LocalTransform = new SharpGLTF.Transforms.AffineTransform(
        scale: Vector3.One,
        rotation: Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2f),
        translation: Vector3.Zero);
    var boneNodes = new NodeBuilder[skelDef.Bones.Count];
    var boneParentIndex = skelDef.CreateParentLookup();

    for (var i = 0; i < skelDef.Bones.Count; i++)
    {
        var bone = skelDef.Bones[i];
        var parentIdx = boneParentIndex[i];
        var parent = parentIdx < 0 ? armature : boneNodes[parentIdx];
        var node = parent.CreateNode(string.IsNullOrEmpty(bone.Name) ? $"bone_{i}" : bone.Name);
        node.LocalMatrix = bone.RestFrame;
        boneNodes[i] = node;
    }

    const string track = "Take 001";

    for (var i = 0; i < animDef.BoneAnims.Count; i++)
    {
        var ba = animDef.BoneAnims[i];
        var node = boneNodes[i];

        if (ba.HasPositionTransform())
        {
            var curve = node.UseTranslation(track);
            foreach (var kv in ba.PositionFrames)
            {
                var v = kv.Value.Value;
                curve.SetPoint(kv.Key / fps, new Vector3(v.X, v.Y, v.Z));
            }
        }
        if (ba.HasRotationTransform())
        {
            var curve = node.UseRotation(track);
            foreach (var kv in ba.RotationFrames)
                curve.SetPoint(kv.Key / fps, kv.Value.Value);
        }
    }

    if (animDef.HasRootPositionTransform())
    {
        var curve = armature.UseTranslation(track);
        foreach (var kv in animDef.RootPositionFrames)
        {
            var v = kv.Value.Value;
            curve.SetPoint(kv.Key / fps, new Vector3(v.X, v.Y, v.Z));
        }
    }

    var scene = new SceneBuilder(animName);
    scene.AddNode(armature);

    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
    var model = scene.ToGltf2();
    if (outPath.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase))
        model.SaveGLTF(outPath);
    else
        model.SaveGLB(outPath);

    Console.WriteLine(outPath);
    Console.WriteLine($"bones={skelDef.Bones.Count} anim-tracks=1 fps={fps} duration={animDef.BoneAnims.Max(b => b.RotationFrames.Count == 0 ? 0f : b.RotationFrames.Keys.Max()) / fps:F3}s");
    return 0;
}

static Skeleton? ResolveSkeleton(AssetPackage pkg, int boneCount, string? requestedName)
{
    var candidates = pkg.Items.OfType<Skeleton>()
        .Where(s => s.Definition is not null && s.Definition.Data.Bones.Count == boneCount)
        .ToList();
    if (candidates.Count == 0) return null;

    if (requestedName is not null)
        return candidates.FirstOrDefault(s => s.Name == requestedName) ?? candidates.First();

    // Prefer canonical-looking biped skeletons (first bone is a pelvis) so repeated runs are stable.
    return candidates.FirstOrDefault(s =>
        {
            var first = s.Definition!.Data.Bones.FirstOrDefault()?.Name;
            return first is not null && first.EndsWith("pelvis", StringComparison.OrdinalIgnoreCase);
        })
        ?? candidates.First();
}

static string DefaultSkeletonsPath(string animTpacPath)
{
    // Walk up to Modules/<any>/..., then over to Modules/Native/AssetPackages/skeletons.tpac
    var dir = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(animTpacPath))!);
    while (dir is not null && dir.Name != "Modules")
        dir = dir.Parent;
    if (dir is null) return "";
    return Path.Combine(dir.FullName, "Native", "AssetPackages", "skeletons.tpac");
}

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
    Console.Error.WriteLine("  tpac-extract <tpac-file> --list               # list textures");
    Console.Error.WriteLine("  tpac-extract <tpac-file> --list-anims         # list skeletal animations");
    Console.Error.WriteLine("  tpac-extract <tpac-file> --list-clips         # list animation-clip actions");
    Console.Error.WriteLine("  tpac-extract <tpac-file> --list-all           # list every asset-item grouped by type");
    Console.Error.WriteLine("  tpac-extract <tpac-file> --dump-anim <anim-name> <output-dir>");
    Console.Error.WriteLine("  tpac-extract <tpac-file> --export-gltf <anim-name> <output.glb> [--skeletons <path>] [--skeleton-name <name>] [--fps 30]");
    Console.Error.WriteLine("  tpac-extract <tpac-file> <texture-name> <output-dir> [--keep-dds]");
}
