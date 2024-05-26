using BinderHandler.Handlers;
using SoulsFormats;

namespace BinderHandler.Guessing
{
    /// <summary>
    /// A folder guesser for many FromSoftware formats that works off of given extensions.
    /// </summary>
    public static class FolderGuesser
    {
        /// <summary>
        /// Guess the folders of all files in a directory by extension, renaming them to use that folders afterwards.
        /// </summary>
        /// <param name="directory">The directory to guess the folders of each file in.</param>
        /// <param name="recursive">Whether or not to search all directories or just the top directory.</param>
        public static void GuessFolders(string directory, bool recursive = false)
        {
            PathExceptionHandler.ThrowIfNotDirectory(directory, nameof(directory));
            var files = Directory.EnumerateFiles(directory, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            foreach (var path in files)
            {
                string newPath = PathHandler.Combine(PathHandler.GetDirectoryName(path), GuessFolder(PathHandler.GetExtensions(path)), Path.GetFileName(path));
                Directory.CreateDirectory(PathHandler.GetDirectoryName(newPath));
                if (!File.Exists(newPath))
                {
                    File.Move(path, newPath);
                }
            }
        }

        /// <summary>
        /// Guess a folder by extension and data stream.
        /// </summary>
        /// <param name="extension">The extension to guess the folder of.</param>
        /// <param name="stream">A data stream in case the original file is an archive that may hold additional context.</param>
        /// <returns>The guessed folder.</returns>
        public static string GuessFolder(string extension, Stream stream)
        {
            if (extension == ".bnd" || extension == ".bhd")
            {
                List<string> extensions = [];
                if (BND3.IsRead(stream, out BND3 bnd3))
                {
                    foreach (var file in bnd3.Files)
                    {
                        extensions.Add(PathHandler.GetExtensions(file.Name));
                    }
                }
                else if (BND4.IsRead(stream, out BND4 bnd4))
                {
                    foreach (var file in bnd4.Files)
                    {
                        extensions.Add(PathHandler.GetExtensions(file.Name));
                    }
                }
                else if (BXF3.IsHeader(stream))
                {
                    var files = BXF3Reader.GetFileHeaders(stream);
                    foreach (var file in files)
                    {
                        extensions.Add(PathHandler.GetExtensions(file.Name));
                    }
                }
                else if (BXF4.IsHeader(stream))
                {
                    var files = BXF4Reader.GetFileHeaders(stream);
                    foreach (var file in files)
                    {
                        extensions.Add(PathHandler.GetExtensions(file.Name));
                    }
                }

                string mostUsedExtension = StringHandler.GetMost(extensions);
                return "bind/" + GuessFolder(mostUsedExtension, true);
            }

            return GuessFolder(extension, false);
        }

        /// <summary>
        /// Guess a folder by extension.
        /// </summary>
        /// <param name="extension">The extension to guess the folder of.</param>
        /// <param name="inArchive">Whether or not the original file this extension came from is in an archive. Used for additional sorting.</param>
        /// <returns>The guessed folder.</returns>
        public static string GuessFolder(string extension, bool inArchive = false)
        {
            if (string.IsNullOrWhiteSpace(extension)) return string.Empty;
            if (extension.IndexOf(".dcx") > 0)
            {
                if (!inArchive)
                {
                    return GuessFolder(extension[..^4]) + "/dcx";
                }

                return GuessFolder(extension[..^4]);
            }

            return extension switch
            {
                ".bnd" => "bind",
                ".bhd" => "bind",
                ".bdt" => "bind",
                ".flv" => "model",
                ".flver" => "model",
                ".smd" => "model",
                ".mdl" => "model",
                ".msb" => "model/map",
                ".nva" => "model/map/ch_nav",
                ".hnav" => "model/map/ch_nav",
                ".htr" => "model/map/ch_nav",
                ".drb" => "lang/menu",
                ".fmg" => "lang/text",
                ".tpf" => "image",
                ".dds" => "image",
                ".png" => "image",
                ".fsb" => "sound",
                ".fev" => "sound",
                ".lua" => "script",
                ".lc" => "script",
                ".evd" => "script",
                ".emevd" => "script",
                ".eld" => "script",
                ".luainfo" => "script",
                ".mtd" => "material",
                ".tae" => "tae",
                ".xml" => "system",
                ".ini" => "system",
                ".txt" => "system",
                ".pem" => "system",
                ".properties" => "system",
                ".param" => "param",
                ".paramdef" => "param/def",
                ".def" => "param/def",
                ".tdf" => "param/tdf",
                ".dbp" => "dbmenu",
                ".pam" => "movie",
                ".ffx" => "sfx",
                _ => PathHandler.GetWithoutExtensionDot(extension).ToLowerInvariant(),
            };
        }
    }
}
