using BCnEncoder.Encoder;
using BCnEncoder.ImageSharp;
using BCnEncoder.Shared;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Bloxstrap.Utility
{
    static class RobloxSkybox
    {
        public const int FaceSize = 512;

        public static readonly IReadOnlyList<string> Faces = new[] { "bk", "dn", "ft", "lf", "rt", "up" };

        public static readonly IReadOnlyDictionary<string, string[]> FaceAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["bk"] = new[] { "bk", "back", "sky512_bk" },
            ["dn"] = new[] { "dn", "down", "bottom", "sky512_dn" },
            ["ft"] = new[] { "ft", "front", "sky512_ft" },
            ["lf"] = new[] { "lf", "left", "sky512_lf" },
            ["rt"] = new[] { "rt", "right", "sky512_rt" },
            ["up"] = new[] { "up", "top", "sky512_up" },
        };

        static string SkyModDirectory => Path.Combine(Paths.Modifications, @"PlatformContent\pc\textures\sky");

        public static string GetModFacePath(string face) => Path.Combine(SkyModDirectory, $"sky512_{face}.tex");

        public static void ApplyPack(CustomSkyPack pack)
        {
            Directory.CreateDirectory(SkyModDirectory);

            foreach (string face in Faces)
            {
                string destinationPath = GetModFacePath(face);
                byte[] texData = ReadFaceTexData(pack, face);

                Filesystem.AssertReadOnly(destinationPath);
                File.WriteAllBytes(destinationPath, texData);
            }
        }

        public static void EnsureAppliedFromSettings()
        {
            const string LOG_IDENT = "RobloxSkybox::EnsureAppliedFromSettings";

            try
            {
                if (!Paths.Initialized)
                    return;

                string packId = App.Settings.Prop.SelectedCustomSkyId;

                if (String.IsNullOrEmpty(packId))
                    return;

                if (IsPackApplied())
                    return;

                var pack = ListInstalledPacks().FirstOrDefault(x => x.Id == packId);

                if (pack is null)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Custom sky pack '{packId}' is selected but was not found");
                    return;
                }

                ApplyPack(pack);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to apply custom sky from settings");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        public static bool IsPackApplied()
        {
            return Faces.All(face => File.Exists(GetModFacePath(face)));
        }

        static byte[] ReadFaceTexData(CustomSkyPack pack, string face)
        {
            string? sourcePath = GetFaceSourcePath(pack.FacesDirectory, face);

            if (sourcePath is null)
                throw new FileNotFoundException($"Missing sky face '{face}'", Path.Combine(pack.FacesDirectory, face));

            string extension = Path.GetExtension(sourcePath).ToLowerInvariant();

            if (extension is ".tex" or ".dds")
                return File.ReadAllBytes(sourcePath);

            return ConvertImageFileToRobloxTex(sourcePath);
        }

        static string? GetFaceSourcePath(string facesDirectory, string face)
        {
            foreach (string extension in new[] { ".tex", ".dds", ".png", ".jpg", ".jpeg", ".bmp" })
            {
                string path = Path.Combine(facesDirectory, $"{face}{extension}");

                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        static bool HasFaceFile(string facesDirectory, string face) => GetFaceSourcePath(facesDirectory, face) is not null;

        public static void RemoveApplied()
        {
            if (!Directory.Exists(SkyModDirectory))
                return;

            foreach (string face in Faces)
            {
                string path = GetModFacePath(face);

                if (!File.Exists(path))
                    continue;

                Filesystem.AssertReadOnly(path);
                File.Delete(path);
            }
        }

        public static byte[] ConvertImageFileToRobloxTex(string imagePath)
        {
            using var image = Image.Load<Rgba32>(imagePath);

            if (image.Width != FaceSize || image.Height != FaceSize)
                image.Mutate(x => x.Resize(FaceSize, FaceSize));

            var encoder = new BcEncoder
            {
                OutputOptions =
                {
                    Format = CompressionFormat.Bc1,
                    FileFormat = OutputFileFormat.Dds,
                    GenerateMipMaps = false,
                    Quality = CompressionQuality.Balanced
                }
            };

            using var output = new MemoryStream();
            encoder.EncodeToStream(image, output);

            return output.ToArray();
        }

        public static bool TryImportSkyPack(string sourceDirectory, string displayName, out CustomSkyPack? pack, out string? errorMessage)
        {
            pack = null;
            errorMessage = null;

            if (!Directory.Exists(sourceDirectory))
            {
                errorMessage = Strings.Menu_Mods_Misc_CustomSky_Invalid;
                return false;
            }

            var resolvedFaces = new Dictionary<string, string>();

            foreach (var pair in FaceAliases)
            {
                string? match = FindFaceFile(sourceDirectory, pair.Value);

                if (match is null)
                {
                    errorMessage = Strings.Menu_Mods_Misc_CustomSky_Invalid;
                    return false;
                }

                resolvedFaces[pair.Key] = match;
            }

            string id = Guid.NewGuid().ToString("N");
            string packDirectory = Path.Combine(Paths.CustomSkies, id);
            string facesDirectory = Path.Combine(packDirectory, "faces");

            Directory.CreateDirectory(facesDirectory);

            foreach (var pair in resolvedFaces)
            {
                string extension = Path.GetExtension(pair.Value).ToLowerInvariant();

                if (extension is ".tex" or ".dds")
                {
                    File.Copy(pair.Value, Path.Combine(facesDirectory, $"{pair.Key}{extension}"), true);
                    continue;
                }

                using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(pair.Value);
                image.Mutate(x => x.Resize(FaceSize, FaceSize));
                image.SaveAsPng(Path.Combine(facesDirectory, $"{pair.Key}.png"));
            }

            string previewSourcePath = Path.Combine(facesDirectory, "up.png");

            if (File.Exists(previewSourcePath))
            {
                using var previewSource = SixLabors.ImageSharp.Image.Load<Rgba32>(previewSourcePath);
                previewSource.SaveAsPng(Path.Combine(packDirectory, "preview.png"));
            }

            var metadata = new CustomSkyPackMetadata
            {
                Name = displayName,
                ImportedAt = DateTime.UtcNow
            };

            File.WriteAllText(
                Path.Combine(packDirectory, "metadata.json"),
                JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true })
            );

            pack = new CustomSkyPack
            {
                Id = id,
                Name = displayName
            };

            return true;
        }

        public static IReadOnlyList<CustomSkyPack> ListInstalledPacks()
        {
            if (!Directory.Exists(Paths.CustomSkies))
                return Array.Empty<CustomSkyPack>();

            var packs = new List<CustomSkyPack>();

            foreach (string directory in Directory.GetDirectories(Paths.CustomSkies))
            {
                string metadataPath = Path.Combine(directory, "metadata.json");
                string facesDirectory = Path.Combine(directory, "faces");

                if (!File.Exists(metadataPath) || !Directory.Exists(facesDirectory))
                    continue;

                if (!Faces.All(face => HasFaceFile(facesDirectory, face)))
                    continue;

                CustomSkyPackMetadata? metadata;

                try
                {
                    metadata = JsonSerializer.Deserialize<CustomSkyPackMetadata>(File.ReadAllText(metadataPath));
                }
                catch
                {
                    continue;
                }

                if (metadata is null || String.IsNullOrWhiteSpace(metadata.Name))
                    continue;

                packs.Add(new CustomSkyPack
                {
                    Id = Path.GetFileName(directory),
                    Name = metadata.Name
                });
            }

            return packs.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static void DeletePack(string packId)
        {
            string directory = Path.Combine(Paths.CustomSkies, packId);

            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        static string? FindFaceFile(string directory, IEnumerable<string> aliases)
        {
            string[] supportedExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".tex", ".dds" };

            foreach (string file in Directory.GetFiles(directory))
            {
                string fileName = Path.GetFileNameWithoutExtension(file);

                if (!aliases.Any(alias => fileName.Equals(alias, StringComparison.OrdinalIgnoreCase)))
                    continue;

                string extension = Path.GetExtension(file);

                if (!supportedExtensions.Any(x => extension.Equals(x, StringComparison.OrdinalIgnoreCase)))
                    continue;

                return file;
            }

            return null;
        }

        class CustomSkyPackMetadata
        {
            public string Name { get; set; } = "";

            public DateTime ImportedAt { get; set; }
        }
    }
}
