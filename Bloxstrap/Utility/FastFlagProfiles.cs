namespace Bloxstrap.Utility
{
    public static class FastFlagProfiles
    {
        public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Profiles { get; } =
            new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["Roblox par défaut"] = new Dictionary<string, string>(),
                ["Performances"] = Profile("1", "0"),
                ["Équilibré"] = Profile("2", "2"),
                ["Qualité visuelle"] = Profile("4", "3")
            };

        private static IReadOnlyDictionary<string, string> Profile(string msaa, string textures) => new Dictionary<string, string>
        {
            ["FIntDebugForceMSAASamples"] = msaa,
            ["DFFlagTextureQualityOverrideEnabled"] = "True",
            ["DFIntTextureQualityOverride"] = textures
        };

        public static void Apply(IDictionary<string, object> flags, string profile)
        {
            if (!Profiles.TryGetValue(profile, out var selected))
                throw new ArgumentException("Préréglage inconnu.", nameof(profile));
            foreach (string key in Profiles.Values.SelectMany(values => values.Keys).Distinct())
                flags.Remove(key);
            foreach (var pair in selected)
                flags[pair.Key] = pair.Value;
        }
    }
}
