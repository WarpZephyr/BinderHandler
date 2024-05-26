using BinderHandler.Handlers;
using SoulsFormats;

namespace BinderHandler
{
    /// <summary>
    /// Unpacks multiple <see cref="BND4"/> files to the same output directory.
    /// </summary>
    /// <param name="destinationDirectory">The directory to unpack all <see cref="BND4"/> files into.</param>
    public class BND4MassUnpacker(string destinationDirectory)
    {
        /// <summary>
        /// The paths to all the <see cref="BND4"/> files to unpack into the same directory.
        /// </summary>
        public List<string> Paths { get; set; } = [];

        /// <summary>
        /// The directory to unpack all <see cref="BND4"/> files into.
        /// </summary>
        public string DestinationDirectory { get; set; } = destinationDirectory;

        /// <summary>
        /// Unpacks all specified <see cref="BND4"/> files to the specified directory.
        /// </summary>
        public void Unpack()
        {
            foreach (var path in Paths)
            {
                PathExceptionHandler.ThrowIfNotFile(path);
                if (BND4.IsRead(path, out BND4 bnd))
                {
                    foreach (var file in bnd.Files)
                    {
                        string writePath = PathHandler.Combine(DestinationDirectory, file.Name);
                        Directory.CreateDirectory(PathHandler.GetDirectoryName(writePath));
                        File.WriteAllBytes(writePath, file.Bytes);
                    }
                }
                else
                {
                    throw new InvalidDataException($"File was not detected as a {nameof(BND4)}: {path}");
                }
            }
        }

        /// <summary>
        /// Unpacks all specified <see cref="BND4"/> files to the specified directory asynchronously.
        /// </summary>
        public async Task UnpackAsync()
        {
            foreach (var path in Paths)
            {
                PathExceptionHandler.ThrowIfNotFile(path);
                if (BND4.IsRead(path, out BND4 bnd))
                {
                    foreach (var file in bnd.Files)
                    {
                        string writePath = PathHandler.Combine(DestinationDirectory, file.Name);
                        Directory.CreateDirectory(PathHandler.GetDirectoryName(writePath));
                        await File.WriteAllBytesAsync(writePath, file.Bytes);
                    }
                }
                else
                {
                    throw new InvalidDataException($"File was not detected as a {nameof(BND4)}: {path}");
                }
            }
        }
    }
}
