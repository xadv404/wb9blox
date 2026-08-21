using System.Text;

using BCnEncoder.Decoder;
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

        static readonly byte[] DdsMagic = Encoding.ASCII.GetBytes("DDS ");

        static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47 };

        static readonly byte[] JpegMagic = { 0xFF, 0xD8, 0xFF };

        const int MaxDdsSearchBytes = 10 * 1024 * 1024;

        public static readonly IReadOnlyList<string> Faces = new[] { "bk", "dn", "ft", "lf", "rt", "up" };

        static readonly string[] SkyModRelativeDirectories =
        {
            @"PlatformContent\pc\textures\sky",
            @"content\textures\sky",
            @"content\sky",
        };

        public static readonly IReadOnlyDictionary<string, string[]> FaceAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["bk"] = new[] { "bk", "back", "sky512_bk" },
            ["dn"] = new[] { "dn", "down", "bottom", "sky512_dn" },
            ["ft"] = new[] { "ft", "front", "sky512_ft" },
            ["lf"] = new[] { "lf", "left", "sky512_lf" },
            ["rt"] = new[] { "rt", "right", "sky512_rt" },
            ["up"] = new[] { "up", "top", "sky512_up" },
        };

        public static string GetModFacePath(string face) => GetModFacePath(SkyModRelativeDirectories[0], face);

        static string GetModFacePath(string relativeDirectory, string face) =>
            Path.Combine(Paths.Modifications, relativeDirectory, $"sky512_{face}.tex");

        public static void ApplyPack(CustomSkyPack pack)
        {
            foreach (string face in Faces)
            {
                byte[] texData = ReadFaceTexData(pack, face);

                foreach (string relativeDirectory in SkyModRelativeDirectories)
                {
                    string destinationPath = GetModFacePath(relativeDirectory, face);
                    WriteTexIfChanged(destinationPath, texData);
                }
            }
        }

        static void WriteTexIfChanged(string destinationPath, byte[] texData)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            if (File.Exists(destinationPath))
            {
                var existingInfo = new FileInfo(destinationPath);

                if (existingInfo.Length == texData.Length)
                {
                    byte[] existingData = File.ReadAllBytes(destinationPath);

                    if (existingData.AsSpan().SequenceEqual(texData))
                        return;
                }
            }

            Filesystem.AssertReadOnly(destinationPath);
            File.WriteAllBytes(destinationPath, texData);
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
                {
                    RemoveApplied();
                    return;
                }

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

        public static void EnsurePreviewCache(CustomSkyPack pack)
        {
            foreach (string face in Faces)
                GetFacePreviewPath(pack, face);
        }

        public static string? GetFacePreviewPath(CustomSkyPack pack, string face)
        {
            string cachedPreviewPath = Path.Combine(pack.DirectoryPath, "preview", $"{face}.png");

            if (File.Exists(cachedPreviewPath))
                return cachedPreviewPath;

            string? sourcePath = GetFaceSourcePath(pack.FacesDirectory, face);

            if (sourcePath is null)
                return null;

            string extension = Path.GetExtension(sourcePath).ToLowerInvariant();

            if (extension is ".png" or ".jpg" or ".jpeg" or ".bmp")
                return sourcePath;

            if (extension is ".tex" or ".dds" && TryWritePreviewFromTex(sourcePath, cachedPreviewPath))
                return cachedPreviewPath;

            return null;
        }

        static byte[] ReadFaceTexData(CustomSkyPack pack, string face)
        {
            string? sourcePath = GetFaceSourcePath(pack.FacesDirectory, face);

            if (sourcePath is null)
                throw new FileNotFoundException($"Missing sky face '{face}'", Path.Combine(pack.FacesDirectory, face));

            string extension = Path.GetExtension(sourcePath).ToLowerInvariant();

            if (extension is ".tex" or ".dds")
                return ReadTexFileData(sourcePath);

            return ConvertImageFileToRobloxTex(sourcePath);
        }

        static byte[] ReadTexFileData(string sourcePath)
        {
            if (ShouldConvertTexToDds(sourcePath, deepScan: true))
                return ConvertImageFileToRobloxTex(sourcePath);

            if (TryGetDdsOffset(sourcePath, out int offset))
            {
                byte[] data = File.ReadAllBytes(sourcePath);
                return data.AsSpan(offset).ToArray();
            }

            return File.ReadAllBytes(sourcePath);
        }

        static string? GetFaceSourcePath(string facesDirectory, string face)
        {
            foreach (string baseName in new[] { $"sky512_{face}", face })
            {
                foreach (string extension in new[] { ".tex", ".dds", ".png", ".jpg", ".jpeg", ".bmp" })
                {
                    string path = Path.Combine(facesDirectory, $"{baseName}{extension}");

                    if (File.Exists(path))
                        return path;
                }
            }

            return null;
        }

        static bool HasFaceFile(string facesDirectory, string face) => GetFaceSourcePath(facesDirectory, face) is not null;

        public static void RemoveApplied()
        {
            foreach (string relativeDirectory in SkyModRelativeDirectories)
            {
                foreach (string face in Faces)
                {
                    string path = GetModFacePath(relativeDirectory, face);

                    if (!File.Exists(path))
                        continue;

                    Filesystem.AssertReadOnly(path);
                    File.Delete(path);
                }
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
                    GenerateMipMaps = true,
                    Quality = CompressionQuality.Balanced
                }
            };

            using var output = new MemoryStream();
            encoder.EncodeToStream(image, output);

            return output.ToArray();
        }

        public static bool TryGetImportIssues(string sourceDirectory, out string? blockingError, out string? confirmMessage)
        {
            blockingError = null;
            confirmMessage = null;

            if (!Directory.Exists(sourceDirectory))
            {
                blockingError = Strings.Menu_Mods_Misc_CustomSky_Invalid;
                return false;
            }

            var unrecognizedTexFiles = new List<string>();

            foreach (var pair in FaceAliases)
            {
                string? match = FindFaceFile(sourceDirectory, pair.Value);

                if (match is null)
                {
                    blockingError = Strings.Menu_Mods_Misc_CustomSky_Invalid;
                    return false;
                }

                string extension = Path.GetExtension(match).ToLowerInvariant();

                if (extension is not (".tex" or ".dds"))
                    continue;

                if (ShouldConvertTexToDds(match, deepScan: true))
                    continue;

                if (!TryGetDdsOffset(match, out _))
                    unrecognizedTexFiles.Add(Path.GetFileName(match));
            }

            if (unrecognizedTexFiles.Count > 0)
            {
                confirmMessage = String.Format(
                    Strings.Menu_Mods_Misc_CustomSky_UnrecognizedTex,
                    String.Join(", ", unrecognizedTexFiles));
            }

            return true;
        }

        public static bool TryImportSkyPack(string sourceDirectory, string displayName, out CustomSkyPack? pack, out string? errorMessage)
        {
            pack = null;
            errorMessage = null;

            if (!TryGetImportIssues(sourceDirectory, out string? blockingError, out _))
            {
                errorMessage = blockingError;
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
                    string destinationTexPath = Path.Combine(facesDirectory, $"sky512_{pair.Key}.tex");

                    if (ShouldConvertTexToDds(pair.Value, deepScan: true))
                    {
                        File.WriteAllBytes(destinationTexPath, ConvertImageFileToRobloxTex(pair.Value));
                        continue;
                    }

                    File.WriteAllBytes(destinationTexPath, ReadTexFileData(pair.Value));
                    continue;
                }

                using var image = Image.Load<Rgba32>(pair.Value);
                image.Mutate(x => x.Resize(FaceSize, FaceSize));
                image.SaveAsPng(Path.Combine(facesDirectory, $"{pair.Key}.png"));
            }

            pack = new CustomSkyPack
            {
                Id = id,
                Name = displayName
            };

            EnsurePreviewCache(pack!);

            string coverPreviewPath = Path.Combine(packDirectory, "preview", "up.png");

            if (File.Exists(coverPreviewPath))
                File.Copy(coverPreviewPath, Path.Combine(packDirectory, "preview.png"), true);

            var metadata = new CustomSkyPackMetadata
            {
                Name = displayName,
                ImportedAt = DateTime.UtcNow
            };

            File.WriteAllText(
                Path.Combine(packDirectory, "metadata.json"),
                JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true })
            );

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

        static bool ShouldConvertTexToDds(string sourcePath, bool deepScan)
        {
            if (HasDdsMagicInPrefix(sourcePath))
                return false;

            if (deepScan && TryGetDdsOffset(sourcePath, out _))
                return false;

            if (IsDisguisedImageFile(sourcePath))
                return true;

            if (!deepScan)
                return false;

            try
            {
                return Image.Identify(sourcePath) is not null;
            }
            catch
            {
                return false;
            }
        }

        static bool HasDdsMagicInPrefix(string sourcePath, int prefixLength = 4096)
        {
            using var input = File.OpenRead(sourcePath);
            int readLength = (int)Math.Min(input.Length, prefixLength);
            byte[] buffer = new byte[readLength];
            int bytesRead = input.Read(buffer, 0, readLength);

            for (int i = 0; i <= bytesRead - DdsMagic.Length; i++)
            {
                if (buffer.AsSpan(i, DdsMagic.Length).SequenceEqual(DdsMagic))
                    return true;
            }

            return false;
        }

        static bool IsDisguisedImageFile(string sourcePath)
        {
            Span<byte> header = stackalloc byte[8];

            using var input = File.OpenRead(sourcePath);

            if (input.Read(header) < PngMagic.Length)
                return false;

            if (header[..PngMagic.Length].SequenceEqual(PngMagic))
                return true;

            if (header[..JpegMagic.Length].SequenceEqual(JpegMagic))
                return true;

            return false;
        }

        static bool TryGetDdsOffset(string sourcePath, out int offset)
        {
            offset = 0;

            byte[] data = File.ReadAllBytes(sourcePath);
            int searchLimit = Math.Min(data.Length, MaxDdsSearchBytes);

            if (searchLimit < DdsMagic.Length)
                return false;

            for (int i = 0; i <= searchLimit - DdsMagic.Length; i++)
            {
                if (data.AsSpan(i, DdsMagic.Length).SequenceEqual(DdsMagic))
                {
                    offset = i;
                    return true;
                }
            }

            return false;
        }

        static bool TryWritePreviewFromTex(string sourcePath, string destinationPath)
        {
            const string LOG_IDENT = "RobloxSkybox::TryWritePreviewFromTex";

            try
            {
                if (!TryGetDdsOffset(sourcePath, out int offset))
                    return false;

                using var input = File.OpenRead(sourcePath);
                input.Seek(offset, SeekOrigin.Begin);

                var decoder = new BcDecoder();
                using var image = decoder.DecodeToImageRgba32(input);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                image.SaveAsPng(destinationPath);
                return true;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Could not decode preview for '{sourcePath}'");
                App.Logger.WriteException(LOG_IDENT, ex);
                return false;
            }
        }

        class CustomSkyPackMetadata
        {
            public string Name { get; set; } = "";

            public DateTime ImportedAt { get; set; }
        }
    }
}
