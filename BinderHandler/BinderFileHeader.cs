using BinderHandler.Handlers;
using BinderHandler.Hashes;
using SoulsFormats;

namespace BinderHandler
{
    /// <summary>
    /// Basic information for a file in a <see cref="Binder"/> archive.
    /// </summary>
    public class BinderFileHeader
    {
        /// <summary>
        /// The full file path to or the name of this file.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The offset of this file inside of an archive.
        /// </summary>
        public long Offset { get; set; }

        /// <summary>
        /// The length of this file inside of an archive.
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// The length of this file plus padding inside of an archive.
        /// </summary>
        public int PaddedLength { get; set; }

        /// <summary>
        /// Hashing information for this file.
        /// </summary>
        public BHD5.SHAHash? SHAHash { get; set; }

        /// <summary>
        /// Encryption information for this file.
        /// </summary>
        public BHD5.AESKey? AESKey { get; set; }

        /// <summary>
        /// Whether or not the file name itself is a file name hash.
        /// </summary>
        public bool NameIsHash { get; set; }

        /// <summary>
        /// Whether or not to ignore this file during unpacking operations.
        /// </summary>
        public bool Ignore { get; set; }

        /// <summary>
        /// Create a <see cref="BinderFileHeader"/> for a file existing on disk.
        /// </summary>
        /// <param name="path">The full file path to the file.</param>
        /// <param name="nameIsHash">Whether or not the file name is a hash.</param>
        public BinderFileHeader(string path, bool nameIsHash = false)
        {
            Path = path;
            Offset = -1;
            Length = -1;
            PaddedLength = -1;
            SHAHash = null;
            AESKey = null;
            NameIsHash = nameIsHash;
            Ignore = false;
        }

        /// <summary>
        /// Create a <see cref="BinderFileHeader"/> for a file inside of an archive.
        /// </summary>
        /// <param name="name">The name of the file.</param>
        /// <param name="offset">The offset the file is at in the archive.</param>
        /// <param name="length">The length of the file in the archive.</param>
        public BinderFileHeader(string name, long offset, int length)
        {
            Path = name;
            Offset = offset;
            Length = length;
            PaddedLength = -1;
            SHAHash = null;
            AESKey = null;
            NameIsHash = false;
            Ignore = false;
        }

        /// <summary>
        /// Create a <see cref="BinderFileHeader"/> for a file inside of an archive.
        /// </summary>
        /// <param name="name">The name of the file.</param>
        /// <param name="offset">The offset the file is at in the archive.</param>
        /// <param name="length">The length of the file in the archive.</param>
        /// <param name="paddedLength">The length of the file inside of the archive plus padding.</param>
        public BinderFileHeader(string name, long offset, int length, int paddedLength)
        {
            Path = name;
            Offset = offset;
            Length = length;
            PaddedLength = paddedLength;
            SHAHash = null;
            AESKey = null;
            NameIsHash = false;
            Ignore = false;
        }

        /// <summary>
        /// Get a file name hash for this file.
        /// </summary>
        /// <param name="rootDirectory">The directory to remove from the path before hashing.</param>
        /// <param name="hash64bit">Whether or not the hash is to be calculated as a 64-bit hash.</param>
        /// <returns>The file name hash of for this file.</returns>
        /// <exception cref="InvalidDataException">The path's file name could not be parsed as a hash when set to do so.</exception>
        public ulong GetFileNameHash(string rootDirectory, bool hash64bit)
        {
            if (NameIsHash)
            {
                if (!ulong.TryParse(PathHandler.GetFileNameWithoutExtensions(Path), out ulong hash))
                {
                    throw new InvalidDataException($"Path file name could not be parsed as hash when set to do so: {Path}");
                }

                return hash;
            }

            return BinderHashDictionary.ComputeHash(PathHandler.GetRelativePath(Path, rootDirectory), hash64bit);
        }

        /// <summary>
        /// Reads this file from the <see cref="Stream"/> it's contained in, decrypting if necessary.
        /// </summary>
        /// <param name="dataStream">The <see cref="Stream"/> containing this file.</param>
        /// <returns>The data of this file.</returns>
        /// <exception cref="Exception">Reading would go beyond the end of the <see cref="Stream"/>.</exception>
        public byte[] ReadFromStream(Stream dataStream)
        {
            // If the AES key for decryption is null, or the padded length is invalid, just use length.
            int length = ((AESKey == null) || (PaddedLength < Length)) ? Length : PaddedLength;
            if (Offset >= dataStream.Length || length > (dataStream.Length - dataStream.Position))
            {
                throw new Exception($"Cannot read beyond end of stream; Offset: {Offset}; Length: {length}; Stream Length: {dataStream.Length}");
            }
            
            dataStream.Position = Offset;
            byte[] buffer = new byte[length];
            dataStream.Read(buffer);
            AESKey?.Decrypt(buffer);
            return buffer;
        }

        /// <summary>
        /// Reads this file from the <see cref="Stream"/> it's contained in asynchronously, decrypting if necessary.
        /// </summary>
        /// <param name="dataStream">The <see cref="Stream"/> containing this file.</param>
        /// <param name="cancellationToken">An object to cancel operations when necessary.</param>
        /// <returns>A <see cref="Task"/> representing the work of getting the data of this file.</returns>
        /// <exception cref="Exception">Reading would go beyond the end of the <see cref="Stream"/>.</exception>
        public async Task<byte[]> ReadFromStreamAsync(Stream dataStream, CancellationToken cancellationToken)
        {
            // If the AES key for decryption is null, or the padded length is invalid, just use length.
            int length = ((AESKey == null) || (PaddedLength < Length)) ? Length : PaddedLength;
            if (Offset >= dataStream.Length || length > (dataStream.Length - Offset))
            {
                throw new Exception($"Cannot read beyond end of stream; Offset: {Offset}; Length: {length}; Stream Length: {dataStream.Length}");
            }

            dataStream.Position = Offset;
            byte[] buffer = new byte[length];
            await dataStream.ReadAsync(buffer, cancellationToken);
            AESKey?.Decrypt(buffer);
            return buffer;
        }
    }
}
