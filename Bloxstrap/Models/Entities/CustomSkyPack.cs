namespace Bloxstrap.Models.Entities
{
    public class CustomSkyPack
    {
        public string Id { get; set; } = "";

        public string Name { get; set; } = "";

        public string DirectoryPath => Path.Combine(Paths.CustomSkies, Id);

        public string FacesDirectory => Path.Combine(DirectoryPath, "faces");

        public string PreviewImagePath => Path.Combine(DirectoryPath, "preview.png");

        public string GetFaceImagePath(string face) => RobloxSkybox.GetFacePreviewPath(this, face) ?? "";

        public override string ToString() => Name;
    }
}
