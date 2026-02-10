using RuriLib.Models.Configs;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RuriLib.Helpers
{
    /// <summary>
    /// Packs and unpacks configs with AES-256-CBC encryption using a password.
    /// The .pbc format: [4 bytes magic "PBC1"] [4 bytes salt length] [salt] [4 bytes IV length] [IV] [encrypted .opk data]
    /// </summary>
    public static class EncryptedConfigPacker
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("PBC1");
        private const int SaltSize = 32;
        private const int KeySize = 32; // 256 bits
        private const int IVSize = 16;  // 128 bits
        private const int Iterations = 100_000;

        /// <summary>
        /// Packs a config into an encrypted .pbc byte array.
        /// </summary>
        public static async Task<byte[]> PackEncryptedAsync(Config config, string password)
        {
            var opkBytes = await ConfigPacker.PackAsync(config);

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var iv = RandomNumberGenerator.GetBytes(IVSize);

            var key = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, KeySize);

            byte[] encryptedData;
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    await cs.WriteAsync(opkBytes);
                }
                encryptedData = ms.ToArray();
            }

            // Build .pbc file: Magic + salt length + salt + IV length + IV + encrypted data
            using var output = new MemoryStream();
            using var writer = new BinaryWriter(output);

            writer.Write(Magic);
            writer.Write(salt.Length);
            writer.Write(salt);
            writer.Write(iv.Length);
            writer.Write(iv);
            writer.Write(encryptedData);

            return output.ToArray();
        }

        /// <summary>
        /// Unpacks a .pbc encrypted config using the provided password.
        /// </summary>
        public static async Task<Config> UnpackEncryptedAsync(byte[] data, string password)
        {
            using var input = new MemoryStream(data);
            using var reader = new BinaryReader(input);

            // Read and verify magic
            var magic = reader.ReadBytes(4);
            if (Encoding.ASCII.GetString(magic) != "PBC1")
                throw new InvalidDataException("Invalid .pbc file format");

            // Read salt
            var saltLength = reader.ReadInt32();
            var salt = reader.ReadBytes(saltLength);

            // Read IV
            var ivLength = reader.ReadInt32();
            var iv = reader.ReadBytes(ivLength);

            // Read encrypted data (rest of stream)
            var encryptedData = reader.ReadBytes((int)(input.Length - input.Position));

            // Derive key
            var key = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, KeySize);

            // Decrypt
            byte[] opkBytes;
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(new MemoryStream(encryptedData), decryptor, CryptoStreamMode.Read))
                {
                    await cs.CopyToAsync(ms);
                }
                opkBytes = ms.ToArray();
            }

            // Unpack the OPK data
            using var opkStream = new MemoryStream(opkBytes);
            return await ConfigPacker.UnpackAsync(opkStream);
        }
    }
}
