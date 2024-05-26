using BinderHandler.Handlers;

namespace BinderHandler.Guessing
{
    /// <summary>
    /// A name guesser that combines guessing the folders and extensions of many FromSoftware file formats.
    /// </summary>
    public static class NameGuesser
    {
        /// <summary>
        /// Guess the folders and extensions of all files in a directory, renaming them to use that folder and extension afterwards.
        /// </summary>
        /// <param name="directory">The directory to guess the folders and extensions of each file in.</param>
        /// <param name="recursive">Whether or not to search all directories or just the top directory.</param>
        public static void GuessNames(string directory, bool recursive = false)
        {
            PathExceptionHandler.ThrowIfNotDirectory(directory, nameof(directory));
            var files = Directory.EnumerateFiles(directory, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            foreach (var path in files)
            {
                string newPath = PathHandler.Combine(PathHandler.GetDirectoryName(path), GuessName(path));
                Directory.CreateDirectory(PathHandler.GetDirectoryName(newPath));
                if (!File.Exists(newPath))
                {
                    File.Move(path, newPath);
                }
            }
        }

        /// <summary>
        /// Guess the folders and extensions of all files in a directory asynchronously, renaming them to use that folder and extension afterwards.
        /// </summary>
        /// <param name="directory">The directory to guess the folders and extensions of each file in.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <param name="recursive">Whether or not to search all directories or just the top directory.</param>
        public async static Task GuessNamesAsync(string directory, CancellationToken cancellationToken, bool recursive = false)
        {
            PathExceptionHandler.ThrowIfNotDirectory(directory, nameof(directory));
            var files = Directory.EnumerateFiles(directory, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            foreach (var path in files)
            {
                if (cancellationToken.IsCancellationRequested) break;
                string guessedName = await GuessNameAsync(path);
                string newPath = PathHandler.Combine(PathHandler.GetDirectoryName(path), guessedName);
                Directory.CreateDirectory(PathHandler.GetDirectoryName(newPath));
                if (!File.Exists(newPath))
                {
                    File.Move(path, newPath);
                }
            }
        }

        /// <summary>
        /// Guess the folder and extension of a file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>The guessed folder and extension of a file.</returns>
        public static string GuessName(string path)
        {
            string filename = Path.GetFileName(path);
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            string extension = ExtensionGuesser.GuessExtension(fs);
            if (string.IsNullOrEmpty(extension))
            {
                return filename;
            }

            string folder = FolderGuesser.GuessFolder(extension, fs);
            return PathHandler.Combine(folder, filename + extension);
        }

        /// <summary>
        /// Guess the folder and extension of a file asynchronously.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>The guessed folder and extension of a file.</returns>
        public async static Task<string> GuessNameAsync(string path)
        {
            string filename = Path.GetFileName(path);
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            string extension = await ExtensionGuesser.GuessExtensionAsync(fs);
            if (string.IsNullOrEmpty(extension))
            {
                return filename;
            }

            string folder = FolderGuesser.GuessFolder(extension, fs);
            return PathHandler.Combine(folder, filename + extension);
        }
    }
}
