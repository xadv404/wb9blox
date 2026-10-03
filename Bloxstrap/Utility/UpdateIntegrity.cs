using System.Security.Cryptography;

namespace Bloxstrap.Utility
{
    public static class UpdateIntegrity
    {
        public static string ParseChecksum(string checksum, string expectedName)
        {
            var match = Regex.Match(checksum.Trim(), @"\A([a-fA-F0-9]{64})[ \t]+\*?([^\r\n]+)\z");
            if (!match.Success || !string.Equals(match.Groups[2].Value, expectedName, StringComparison.Ordinal))
                throw new InvalidDataException("Somme de contrôle de mise à jour invalide.");
            return match.Groups[1].Value;
        }

        public static async Task VerifyAsync(string path, string expectedHash)
        {
            if (!Regex.IsMatch(expectedHash, @"\A[a-fA-F0-9]{64}\z"))
                throw new InvalidDataException("Empreinte SHA-256 invalide.");
            using var algorithm = SHA256.Create();
            await using var stream = File.OpenRead(path);
            var actual = await algorithm.ComputeHashAsync(stream);
            if (!CryptographicOperations.FixedTimeEquals(actual, Convert.FromHexString(expectedHash)))
                throw new InvalidDataException("L’intégrité de la mise à jour n’a pas pu être vérifiée.");
        }
    }
}
