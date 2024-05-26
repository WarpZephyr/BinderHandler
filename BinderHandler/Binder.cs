using BinderHandler.Handlers;
using BinderHandler.Hashes;
using BinderHandler.Strategy;
using SoulsFormats;
using System.Text;

namespace BinderHandler
{
    /// <summary>
    /// An generic object for FromSoftware Binder archives.
    /// </summary>
    public class Binder
    {
        /// <summary>
        /// The max amount to be writing at once while unpacking a data archive asynchronously.
        /// </summary>
        private const long MAX_WRITING_SIZE = 1024 * 1024 * 100;

        /// <summary>
        /// A version identifier for the current iteration of the <see cref="Binder"/>.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Whether or not the <see cref="Binder"/> uses big-endian byte ordering.
        /// </summary>
        public bool BigEndian { get; set; }

        /// <summary>
        /// The directory that all files start from.
        /// <para>Used during writing to determine naming.</para>
        /// </summary>
        public string RootDirectory { get; set; }

        /// <summary>
        /// An object that holds information on how buckets should be handled.
        /// </summary>
        public BucketInfo? BucketInfo { get; set; }

        /// <summary>
        /// Whether or not to skip writing unknown files when unpacking name hashed archives.
        /// </summary>
        public bool SkipUnknownFiles { get; set; }

        /// <summary>
        /// Whether or not to skip writing existing files when unpacking archives.
        /// </summary>
        public bool SkipExistingFiles { get; set; }

        /// <summary>
        /// The files to be stored in the <see cref="Binder"/>.
        /// </summary>
        public List<BinderFileHeader> Files { get; set; }

        /// <summary>
        /// Creates an empty <see cref="Binder"/>.
        /// </summary>
        public Binder()
        {
            Version = string.Empty;
            BigEndian = false;
            RootDirectory = string.Empty;
            BucketInfo = null;
            SkipExistingFiles = false;
            SkipUnknownFiles = false;
            Files = [];
        }

        /// <summary>
        /// Creates a <see cref="Binder"/> with the specified list of files.
        /// </summary>
        /// <param name="files">A list of files.</param>
        public Binder(List<string> files)
        {
            Version = string.Empty;
            BigEndian = false;
            RootDirectory = string.Empty;
            BucketInfo = null;
            SkipExistingFiles = false;
            SkipUnknownFiles = false;
            Files = [];
            foreach (var file in files)
            {
                Files.Add(new BinderFileHeader(file));
            }
        }

        /// <summary>
        /// Creates a <see cref="Binder"/> with the specified list of files.
        /// </summary>
        /// <param name="files">A list of files.</param>
        public Binder(List<BinderFileHeader> files)
        {
            Version = string.Empty;
            BigEndian = false;
            RootDirectory = string.Empty;
            BucketInfo = null;
            SkipExistingFiles = false;
            SkipUnknownFiles = false;
            Files = files;
        }

        #region Selection Methods

        /// <summary>
        /// Set files not in the provided list to be ignored.
        /// </summary>
        /// <param name="selectedFiles">The selected list of files.</param>
        public void SetSelectedFiles(IList<string> selectedFiles)
        {
            foreach (var file in Files)
            {
                if (!selectedFiles.Contains(file.Path))
                {
                    file.Ignore = true;
                }
                else
                {
                    file.Ignore = false;
                }
            }
        }

        /// <summary>
        /// Whether or not all files in this <see cref="Binder"/> are set to be ignored.
        /// </summary>
        /// <returns><see langword="true"/> if all files are set to be ignored or if there are no files; <see langword="false"/> if no files were set to be ignored.</returns>
        public bool AllFilesIgnored()
        {
            foreach (var file in Files)
            {
                if (!file.Ignore)
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region Write BinderHeader5 Methods

        /// <summary>
        /// Writes this <see cref="Binder"/> to a binder header 5 split archive.
        /// </summary>
        /// <param name="outputDirectory">The directory to output the archive to.</param>
        /// <param name="headerName">The name of the header file of the archive.</param>
        /// <param name="dataName">The name of the data file of the archive.</param>
        /// <param name="formatVersion">The version of the format of the archive.</param>
        /// <param name="alignment">The alignment to use for each file in the archive.</param>
        /// <param name="writeDataHeader">Whether or not to write a data header inside the data file.</param>
        public void WriteToBinderHeader5(string outputDirectory, string headerName, string dataName, BHD5.Game formatVersion, long alignment = 0, bool writeDataHeader = false)
        {
            PathExceptionHandler.ThrowIfFile(outputDirectory, nameof(outputDirectory));
            string headerPath = PathHandler.Combine(outputDirectory, headerName);
            PathExceptionHandler.ThrowIfDirectory(headerPath, nameof(headerPath));
            string dataPath = PathHandler.Combine(outputDirectory, dataName);
            PathExceptionHandler.ThrowIfDirectory(dataPath, nameof(dataPath));

            var header = new BHD5(formatVersion);
            InitializeBuckets(header, GetBucketCount());
            using var dataStream = new FileStream(dataPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            if (writeDataHeader)
            {
                dataStream.Write(GetDataHeader(formatVersion, Version));
            }

            foreach (var file in Files)
            {
                PathExceptionHandler.ThrowIfNotFile(file.Path, nameof(file.Path));
                ulong hash = file.GetFileNameHash(RootDirectory, formatVersion >= BHD5.Game.EldenRing);
                int bucketIndex = GetBucketIndex(hash);

                long length = new FileInfo(file.Path).Length;
                var fileHeader = new BHD5.FileHeader();
                fileHeader.FileNameHash = hash;
                fileHeader.FileOffset = dataStream.Position;

                using var fs = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
                fs.CopyTo(dataStream);

                if (alignment > 1)
                {
                    Pad(dataStream, alignment);
                }

                fileHeader.UnpaddedFileSize = length;
                fileHeader.PaddedFileSize = (int)(dataStream.Position - fileHeader.FileOffset);
                header.Buckets[bucketIndex].Add(fileHeader);
            }

            header.BigEndian = BigEndian;
            header.Write(headerPath);
        }

        /// <summary>
        /// Writes this <see cref="Binder"/> to a binder header 5 split archive asynchronously.
        /// </summary>
        /// <param name="outputDirectory">The directory to output the archive to.</param>
        /// <param name="headerName">The name of the header file of the archive.</param>
        /// <param name="dataName">The name of the data file of the archive.</param>
        /// <param name="formatVersion">The version of the format of the archive.</param>
        /// <param name="bigEndian">Whether or not the archive should be written in big endian byte ordering.</param>
        /// <param name="alignment">The alignment to use for each file in the archive.</param>
        /// <param name="writeDataHeader">Whether or not to write a data header inside the data file.</param>
        /// <param name="progress">An object to report the progress of writing.</param>
        /// <param name="cancellationToken">An object to cancel operations when necessary.</param>
        /// <returns>A <see cref="Task"/> representing the work of writing.</returns>
        public async Task WriteToBinderHeader5Async(string outputDirectory, string headerName, string dataName, BHD5.Game formatVersion, bool bigEndian, long alignment, bool writeDataHeader, IProgress<double> progress, CancellationToken cancellationToken)
        {
            PathExceptionHandler.ThrowIfFile(outputDirectory, nameof(outputDirectory));
            string headerPath = PathHandler.Combine(outputDirectory, headerName);
            PathExceptionHandler.ThrowIfDirectory(headerPath, nameof(headerPath));
            string dataPath = PathHandler.Combine(outputDirectory, dataName);
            PathExceptionHandler.ThrowIfDirectory(dataPath, nameof(dataPath));

            var header = new BHD5(formatVersion);
            InitializeBuckets(header, GetBucketCount());
            using var dataStream = new FileStream(dataPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            if (writeDataHeader)
            {
                dataStream.Write(GetDataHeader(formatVersion, Version));
            }

            double fileCountProgress = Files.Count;
            int fileNum = 1;
            foreach (var file in Files)
            {
                if (cancellationToken.IsCancellationRequested) break;
                progress.Report(fileNum / fileCountProgress);

                PathExceptionHandler.ThrowIfNotFile(file.Path, nameof(file.Path));
                ulong hash = file.GetFileNameHash(RootDirectory, formatVersion >= BHD5.Game.EldenRing);
                int bucketIndex = GetBucketIndex(hash);

                long length = new FileInfo(file.Path).Length;
                var fileHeader = new BHD5.FileHeader();
                fileHeader.FileNameHash = hash;
                fileHeader.FileOffset = dataStream.Position;

                using var fs = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
                await fs.CopyToAsync(dataStream, cancellationToken);

                if (alignment > 1)
                {
                    Pad(dataStream, alignment);
                }

                fileHeader.UnpaddedFileSize = length;
                fileHeader.PaddedFileSize = (int)(dataStream.Position - fileHeader.FileOffset);
                header.Buckets[bucketIndex].Add(fileHeader);
                fileNum++;
            }

            if (cancellationToken.IsCancellationRequested) return;

            header.BigEndian = bigEndian;
            header.Write(headerPath);
        }

        #endregion

        #region Unpack Data Methods

        /// <summary>
        /// Unpacks the <see cref="Binder"/> from the data file containing its data.
        /// </summary>
        /// <param name="dataPath">The path to a data file containing the data of this <see cref="Binder"/>.</param>
        /// <param name="outputDirectory">The directory to write unpacked data to.</param>
        public void UnpackDataFromPath(string dataPath, string outputDirectory)
        {
            using var fs = new FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            UnpackData(fs, outputDirectory);
        }

        /// <summary>
        /// Unpacks the <see cref="Binder"/> from the data file containing its data asynchronously.
        /// </summary>
        /// <param name="dataPath">The path to a data file containing the data of this <see cref="Binder"/>.</param>
        /// <param name="outputDirectory">The directory to write unpacked data to.</param>
        /// <param name="progress">An object to report the progress of writing.</param>
        /// <param name="cancellationToken">An object to cancel operations when necessary.</param>
        public async Task UnpackDataFromPathAsync(string dataPath, string outputDirectory, IProgress<double> progress, CancellationToken cancellationToken)
        {
            using var fs = new FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await UnpackDataAsync(fs, outputDirectory, progress, cancellationToken);
        }

        /// <summary>
        /// Unpacks the <see cref="Binder"/> from the <see cref="Stream"/> containing its data.
        /// </summary>
        /// <param name="dataStream">A <see cref="Stream"/> containing the data of this <see cref="Binder"/>.</param>
        /// <param name="outputDirectory">The directory to write unpacked data to.</param>
        public void UnpackData(Stream dataStream, string outputDirectory)
        {
            PathExceptionHandler.ThrowIfFile(outputDirectory, nameof(outputDirectory));
            Directory.CreateDirectory(outputDirectory);
            foreach (var file in Files)
            {
                if (file.Ignore || SkipUnknownFiles && file.NameIsHash) continue;

                string writePath = PathHandler.Combine(outputDirectory, file.Path);
                if (SkipExistingFiles && File.Exists(writePath)) continue;

                PathExceptionHandler.ThrowIfDirectory(writePath, nameof(writePath));
                string? directoryName = Path.GetDirectoryName(writePath);
                if (!string.IsNullOrEmpty(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                using var fs = new FileStream(writePath, FileMode.Create, FileAccess.Read, FileShare.Read);
                fs.Write(file.ReadFromStream(dataStream));
            }
        }

        /// <summary>
        /// Unpacks the <see cref="Binder"/> from the <see cref="Stream"/> containing its data asynchronously.
        /// </summary>
        /// <param name="dataStream">A <see cref="Stream"/> containing the data of this <see cref="Binder"/>.</param>
        /// <param name="outputDirectory">The directory to write unpacked data to.</param>
        /// <param name="progress">An object to report the progress of writing.</param>
        /// <param name="cancellationToken">An object to cancel operations when necessary.</param>
        public async Task UnpackDataAsync(Stream dataStream, string outputDirectory, IProgress<double> progress, CancellationToken cancellationToken)
        {
            PathExceptionHandler.ThrowIfFile(outputDirectory, nameof(outputDirectory));
            Directory.CreateDirectory(outputDirectory);

            long writingSize = 0;
            var writerTasks = new List<Task<long>>();
            double fileCountProgress = Files.Count;
            int fileNum = 1;
            foreach (var file in Files)
            {
                if (cancellationToken.IsCancellationRequested) break;
                progress.Report(fileNum / fileCountProgress);

                if (file.Ignore || SkipUnknownFiles && file.NameIsHash) continue;

                string writePath = PathHandler.Combine(outputDirectory, file.Path);
                if (SkipExistingFiles && File.Exists(writePath)) continue;

                PathExceptionHandler.ThrowIfDirectory(writePath, nameof(writePath));

                if (writerTasks.Count > 0 && (writingSize + file.Length) > MAX_WRITING_SIZE)
                {
                    for (int i = 0; i < writerTasks.Count; i++)
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        if (writerTasks[i].IsCompleted)
                        {
                            writingSize -= await writerTasks[i];
                            writerTasks.RemoveAt(i);
                        }
                    }
                }

                byte[] bytes = await file.ReadFromStreamAsync(dataStream, cancellationToken);
                writerTasks.Add(WriteToPathAsync(bytes, writePath, cancellationToken));
                fileNum++;
            }

            foreach (var task in writerTasks)
            {
                await task;
            }
        }

        #endregion

        #region SoulsFormats Conversion Methods

        /// <summary>
        /// Converts this <see cref="Binder"/> into a <see cref="BND3"/>.
        /// </summary>
        /// <returns>A <see cref="BND3"/>.</returns>
        public BND3 ToBND3()
        {
            var binder = new BND3
            {
                Version = Version,
                BigEndian = BigEndian
            };

            foreach (var file in Files)
            {
                var binderfile = new BinderFile
                {
                    Name = GetFileName(file.Path),
                    Bytes = File.ReadAllBytes(file.Path)
                };

                binder.Files.Add(binderfile);
            }

            return binder;
        }

        /// <summary>
        /// Converts this <see cref="Binder"/> into a <see cref="BND4"/>.
        /// </summary>
        /// <returns>A <see cref="BND4"/>.</returns>
        public BND4 ToBND4()
        {
            var binder = new BND4
            {
                Version = Version,
                BigEndian = BigEndian
            };

            foreach (var file in Files)
            {
                var binderfile = new BinderFile
                {
                    Name = GetFileName(file.Path),
                    Bytes = File.ReadAllBytes(file.Path)
                };

                binder.Files.Add(binderfile);
            }

            return binder;
        }

        /// <summary>
        /// Converts this <see cref="Binder"/> into a <see cref="BXF3"/>.
        /// </summary>
        /// <returns>A <see cref="BXF3"/>.</returns>
        public BXF3 ToBXF3()
        {
            var binder = new BXF3
            {
                Version = Version,
                BigEndian = BigEndian
            };

            foreach (var file in Files)
            {
                var binderfile = new BinderFile
                {
                    Name = GetFileName(file.Path),
                    Bytes = File.ReadAllBytes(file.Path)
                };

                binder.Files.Add(binderfile);
            }

            return binder;
        }

        /// <summary>
        /// Converts this <see cref="Binder"/> into a <see cref="BXF4"/>.
        /// </summary>
        /// <returns>A <see cref="BXF4"/>.</returns>
        public BXF4 ToBXF4()
        {
            var binder = new BXF4
            {
                Version = Version,
                BigEndian = BigEndian
            };

            foreach (var file in Files)
            {
                var binderfile = new BinderFile
                {
                    Name = GetFileName(file.Path),
                    Bytes = File.ReadAllBytes(file.Path)
                };

                binder.Files.Add(binderfile);
            }

            return binder;
        }

        #endregion

        #region Instantiation Factory Methods

        /// <summary>
        /// Creates a <see cref="Binder"/> from the files in a directory.
        /// </summary>
        /// <param name="inputDirectory">The directory to get file entries from.</param>
        /// <returns>A <see cref="Binder"/>.</returns>
        public static Binder FromDirectory(string inputDirectory)
        {
            PathExceptionHandler.ThrowIfNotDirectory(inputDirectory, nameof(inputDirectory));
            var binder = new Binder();

            var files = Directory.EnumerateFiles(inputDirectory, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                binder.Files.Add(new BinderFileHeader(file));
            }

            return binder;
        }

        /// <summary>
        /// Creates a <see cref="Binder"/> from files in a directory, and hashed name files in another directory.
        /// </summary>
        /// <param name="inputDirectory">The directory to get file entries from.</param>
        /// <param name="hashedNamesDirectory">The directory to get hashed name file entries from.</param>
        /// <returns>A <see cref="Binder"/>.</returns>
        public static Binder FromDirectories(string inputDirectory, string hashedNamesDirectory)
        {
            PathExceptionHandler.ThrowIfNotDirectory(inputDirectory, nameof(inputDirectory));
            PathExceptionHandler.ThrowIfNotDirectory(hashedNamesDirectory, nameof(hashedNamesDirectory));
            var binder = new Binder();

            var files = Directory.EnumerateFiles(inputDirectory, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (!file.StartsWith(hashedNamesDirectory))
                {
                    binder.Files.Add(new BinderFileHeader(file, false));
                }
            }

            var hashNamedFiles = Directory.EnumerateFiles(hashedNamesDirectory, "*", SearchOption.AllDirectories);
            foreach (var file in hashNamedFiles)
            {
                binder.Files.Add(new BinderFileHeader(file, true));
            }

            return binder;
        }

        /// <summary>
        /// Creates a <see cref="Binder"/> from the given binder header 5 and <see cref="BinderHashDictionary"/>.
        /// </summary>
        /// <param name="header">A binder header 5.</param>
        /// <param name="hashDictionary">A <see cref="BinderHashDictionary"/> possibly containing the names of files.</param>
        /// <returns>A <see cref="Binder"/>.</returns>
        public static Binder FromBinderHeader5(BHD5 header, BinderHashDictionary hashDictionary)
        {
            var binder = new Binder();
            foreach (var bucket in header.Buckets)
            {
                foreach (var file in bucket)
                {
                    var fileHeader = new BinderFileHeader(GetFileNameFromBinderHash(file.FileNameHash, hashDictionary, out bool nameIsHash));
                    fileHeader.NameIsHash = nameIsHash;
                    fileHeader.Length = GetFileLength(file, header.Format);
                    fileHeader.PaddedLength = file.PaddedFileSize;
                    fileHeader.Offset = file.FileOffset;
                    fileHeader.SHAHash = file.SHAHash;
                    fileHeader.AESKey = file.AESKey;
                    binder.Files.Add(fileHeader);
                }
            }

            return binder;
        }

        #endregion

        #region Unpack Factory Methods

        /// <summary>
        /// Unpacks a binder header 5 from the given path information.
        /// </summary>
        /// <param name="headerPath">The path to the header file.</param>
        /// <param name="dataPath">The path to the data file.</param>
        /// <param name="dictionaryPath">The path to the dictionary file.</param>
        /// <param name="outputDirectory">The directory to write unpacked files to.</param>
        /// <param name="formatVersion">The version of the format of the header.</param>
        /// <param name="decryptionKey">A decryption key for the header if applicable.</param>
        public static void UnpackFromPaths(string headerPath, string dataPath, string dictionaryPath, string outputDirectory, BHD5.Game formatVersion, string? decryptionKey)
        {
            PathExceptionHandler.ThrowIfNotFile(dictionaryPath, nameof(dictionaryPath));
            UnpackFromPaths(headerPath, dataPath, outputDirectory, BinderHashDictionary.FromPath(dictionaryPath, formatVersion >= BHD5.Game.EldenRing), formatVersion, decryptionKey);
        }

        /// <summary>
        /// Unpacks a binder header 5 from the given path information.
        /// </summary>
        /// <param name="headerPath">The path to the header file.</param>
        /// <param name="dataPath">The path to the data file.</param>
        /// <param name="hashDictionary">A <see cref="BinderHashDictionary"/> possibly containing names for files.</param>
        /// <param name="outputDirectory">The directory to write unpacked files to.</param>
        /// <param name="formatVersion">The version of the format of the header.</param>
        /// <param name="decryptionKey">A decryption key for the header if applicable.</param>
        public static void UnpackFromPaths(string headerPath, string dataPath, string outputDirectory, BinderHashDictionary hashDictionary, BHD5.Game formatVersion, string? decryptionKey)
            => FromBinderHeader5(ReadBHD5(headerPath, decryptionKey, formatVersion), hashDictionary).UnpackDataFromPath(dataPath, outputDirectory);

        /// <summary>
        /// Unpacks a binder header 5 from the given path information asynchronously.
        /// </summary>
        /// <param name="headerPath">The path to the header file.</param>
        /// <param name="dataPath">The path to the data file.</param>
        /// <param name="dictionaryPath">The path to the dictionary file.</param>
        /// <param name="outputDirectory">The directory to write unpacked files to.</param>
        /// <param name="formatVersion">The version of the format of the header.</param>
        /// <param name="decryptionKey">A decryption key for the header if applicable.</param>
        /// <param name="progress">An object to report the progress of unpacking.</param>
        /// <param name="cancellationToken">An object to cancel operations when necessary.</param>
        /// <returns>A <see cref="Task"/> representing the work of unpacking.</returns>
        public async static Task UnpackFromPathsAsync(string headerPath, string dataPath, string dictionaryPath, string outputDirectory,
            BHD5.Game formatVersion, string? decryptionKey, IProgress<double> progress, CancellationToken cancellationToken)
        {
            PathExceptionHandler.ThrowIfNotFile(dictionaryPath, nameof(dictionaryPath));
            await UnpackFromPathsAsync(headerPath, dataPath, outputDirectory, BinderHashDictionary.FromPath(dictionaryPath, formatVersion >= BHD5.Game.EldenRing), formatVersion, decryptionKey, progress, cancellationToken);
        }

        /// <summary>
        /// Unpacks a binder header 5 from the given path information asynchronously.
        /// </summary>
        /// <param name="headerPath">The path to the header file.</param>
        /// <param name="dataPath">The path to the data file.</param>
        /// <param name="hashDictionary">A <see cref="BinderHashDictionary"/> possibly containing names for files.</param>
        /// <param name="outputDirectory">The directory to write unpacked files to.</param>
        /// <param name="formatVersion">The version of the format of the header.</param>
        /// <param name="decryptionKey">A decryption key for the header if applicable.</param>
        /// <param name="progress">An object to report the progress of unpacking.</param>
        /// <param name="cancellationToken">An object to cancel operations when necessary.</param>
        /// <returns>A <see cref="Task"/> representing the work of unpacking.</returns>
        public async static Task UnpackFromPathsAsync(string headerPath, string dataPath, string outputDirectory, BinderHashDictionary hashDictionary,
            BHD5.Game formatVersion, string? decryptionKey, IProgress<double> progress, CancellationToken cancellationToken)
            => await FromBinderHeader5(ReadBHD5(headerPath, decryptionKey, formatVersion), hashDictionary).UnpackDataFromPathAsync(dataPath, outputDirectory, progress, cancellationToken);

        #endregion

        #region Repack Factory Methods

        /// <summary>
        /// Write files found from the given pathing information to a binder header 5 archive.
        /// </summary>
        /// <param name="inputDirectory">The directory to get files from.</param>
        /// <param name="hashedNamesDirectory">The directory to get files that have hashed names from.</param>
        /// <param name="outputDirectory">The directory to output the archive to.</param>
        /// <param name="headerName">The name of the header file of the archive.</param>
        /// <param name="dataName">The name of the data file of the archive.</param>
        /// <param name="formatVersion">The version of the format of the archive.</param>
        /// <param name="bigEndian">Whether or not the archive is to be written in big endian.</param>
        public static void WriteDirectoriesIntoBinderHeader5(string inputDirectory, string hashedNamesDirectory, string outputDirectory, string headerName, string dataName, BHD5.Game formatVersion, bool bigEndian)
            => WriteDirectoriesIntoBinderHeader5(inputDirectory, hashedNamesDirectory, outputDirectory, headerName, dataName, formatVersion, bigEndian, 0, false, string.Empty, 7);

        /// <summary>
        /// Write files found from the given pathing information to a binder header 5 archive.
        /// </summary>
        /// <param name="inputDirectory">The directory to get files from.</param>
        /// <param name="outputDirectory">The directory to output the archive to.</param>
        /// <param name="headerName">The name of the header file of the archive.</param>
        /// <param name="dataName">The name of the data file of the archive.</param>
        /// <param name="formatVersion">The version of the format of the archive.</param>
        /// <param name="bigEndian">Whether or not the archive is to be written in big endian.</param>
        public static void WriteDirectoryIntoBinderHeader5(string inputDirectory, string outputDirectory, string headerName, string dataName, BHD5.Game formatVersion, bool bigEndian)
            => WriteDirectoryIntoBinderHeader5(inputDirectory, outputDirectory, headerName, dataName, formatVersion, bigEndian, 0, false, string.Empty, 7);

        /// <summary>
        /// Write files found from the given pathing information to a binder header 5 archive asynchronously.
        /// </summary>
        /// <param name="inputDirectory">The directory to get files from.</param>
        /// <param name="hashedNamesDirectory">The directory to get files that have hashed names from.</param>
        /// <param name="outputDirectory">The directory to output the archive to.</param>
        /// <param name="headerName">The name of the header file of the archive.</param>
        /// <param name="dataName">The name of the data file of the archive.</param>
        /// <param name="formatVersion">The version of the format of the archive.</param>
        /// <param name="bigEndian">Whether or not the archive is to be written in big endian.</param>
        /// <param name="progress">An object to report the progress of writing.</param>
        /// <param name="cancellationToken">An object to cancel operations when necessary.</param>
        public async static Task WriteDirectoriesIntoBinderHeader5Async(string inputDirectory, string hashedNamesDirectory, string outputDirectory, string headerName, string dataName, BHD5.Game formatVersion, bool bigEndian, IProgress<double> progress, CancellationToken cancellationToken)
            => await WriteDirectoriesIntoBinderHeader5Async(inputDirectory, hashedNamesDirectory, outputDirectory, headerName, dataName, formatVersion, bigEndian, 0, false, string.Empty, 7, progress, cancellationToken);

        /// <summary>
        /// Write files found from the given pathing information to a binder header 5 archive asynchronously.
        /// </summary>
        /// <param name="inputDirectory">The directory to get files from.</param>
        /// <param name="outputDirectory">The directory to output the archive to.</param>
        /// <param name="headerName">The name of the header file of the archive.</param>
        /// <param name="dataName">The name of the data file of the archive.</param>
        /// <param name="formatVersion">The version of the format of the archive.</param>
        /// <param name="bigEndian">Whether or not the archive is to be written in big endian.</param>
        /// <param name="progress">An object to report the progress of writing.</param>
        /// <param name="cancellationToken">An object to cancel operations when necessary.</param>
        public async static Task WriteDirectoryIntoBinderHeader5Async(string inputDirectory, string outputDirectory, string headerName, string dataName, BHD5.Game formatVersion, bool bigEndian, IProgress<double> progress, CancellationToken cancellationToken)
            => await WriteDirectoriesIntoBinderHeader5Async(inputDirectory, outputDirectory, headerName, dataName, formatVersion, bigEndian, 0, false, string.Empty, 7, progress, cancellationToken);

        /// <summary>
        /// Write files found from the given information to a binder header 5 archive.
        /// </summary>
        /// <param name="inputDirectory">The directory to get files from.</param>
        /// <param name="hashedNamesDirectory">The directory to get files that have hashed names from.</param>
        /// <param name="outputDirectory">The directory to output the archive to.</param>
        /// <param name="headerName">The name of the header file of the archive.</param>
        /// <param name="dataName">The name of the data file of the archive.</param>
        /// <param name="formatVersion">The version of the format of the archive.</param>
        /// <param name="bigEndian">Whether or not the archive is to be written in big endian.</param>
        /// <param name="alignment">The alignment to use for each file in the archive.</param>
        /// <param name="writeDataHeader">Whether or not to write a data header inside the data file.</param>
        /// <param name="dataHeaderVersion">The version string to set in the data header.</param>
        /// <param name="bucketDistribution">The average number of files per bucket.</param>
        public static void WriteDirectoriesIntoBinderHeader5(string inputDirectory, string hashedNamesDirectory, string outputDirectory, string headerName, string dataName,
            BHD5.Game formatVersion, bool bigEndian, long alignment, bool writeDataHeader, string dataHeaderVersion, int bucketDistribution)
        {
            var binder = FromDirectories(inputDirectory, hashedNamesDirectory);
            binder.Version = dataHeaderVersion;
            binder.BigEndian = bigEndian;
            var countStrategy = new DistributionBucketCountStrategy(bucketDistribution, binder.Files.Count);
            var indexStrategy = new ModulusBucketIndexStrategy(countStrategy.ComputeBucketCount());
            binder.BucketInfo = new BucketInfo(countStrategy, indexStrategy);
            binder.WriteToBinderHeader5(outputDirectory, headerName, dataName, formatVersion, alignment, writeDataHeader);
        }

        /// <summary>
        /// Write files found from the given information to a binder header 5 archive.
        /// </summary>
        /// <param name="inputDirectory">The directory to get files from.</param>
        /// <param name="outputDirectory">The directory to output the archive to.</param>
        /// <param name="headerName">The name of the header file of the archive.</param>
        /// <param name="dataName">The name of the data file of the archive.</param>
        /// <param name="formatVersion">The version of the format of the archive.</param>
        /// <param name="bigEndian">Whether or not the archive is to be written in big endian.</param>
        /// <param name="alignment">The alignment to use for each file in the archive.</param>
        /// <param name="writeDataHeader">Whether or not to write a data header inside the data file.</param>
        /// <param name="dataHeaderVersion">The version string to set in the data header.</param>
        /// <param name="bucketDistribution">The average number of files per bucket.</param>
        public static void WriteDirectoryIntoBinderHeader5(string inputDirectory, string outputDirectory, string headerName, string dataName,
            BHD5.Game formatVersion, bool bigEndian, long alignment, bool writeDataHeader, string dataHeaderVersion, int bucketDistribution)
        {
            var binder = FromDirectory(inputDirectory);
            binder.Version = dataHeaderVersion;
            binder.BigEndian = bigEndian;
            var countStrategy = new DistributionBucketCountStrategy(bucketDistribution, binder.Files.Count);
            var indexStrategy = new ModulusBucketIndexStrategy(countStrategy.ComputeBucketCount());
            binder.BucketInfo = new BucketInfo(countStrategy, indexStrategy);
            binder.WriteToBinderHeader5(outputDirectory, headerName, dataName, formatVersion, alignment, writeDataHeader);
        }

        /// <summary>
        /// Write files found from the given information to a binder header 5 archive asynchronously.
        /// </summary>
        /// <param name="inputDirectory">The directory to get files from.</param>
        /// <param name="hashedNamesDirectory">The directory to get files that have hashed names from.</param>
        /// <param name="outputDirectory">The directory to output the archive to.</param>
        /// <param name="headerName">The name of the header file of the archive.</param>
        /// <param name="dataName">The name of the data file of the archive.</param>
        /// <param name="formatVersion">The version of the format of the archive.</param>
        /// <param name="bigEndian">Whether or not the archive is to be written in big endian.</param>
        /// <param name="alignment">The alignment to use for each file in the archive.</param>
        /// <param name="writeDataHeader">Whether or not to write a data header inside the data file.</param>
        /// <param name="dataHeaderVersion">The version string to set in the data header.</param>
        /// <param name="bucketDistribution">The average number of files per bucket.</param>
        /// <param name="progress">An object to report the progress of writing.</param>
        /// <param name="cancellationToken">An object to cancel operations when necessary.</param>
        /// <returns>A <see cref="Task"/> representing the work of writing.</returns>
        public async static Task WriteDirectoriesIntoBinderHeader5Async(string inputDirectory, string hashedNamesDirectory, string outputDirectory, string headerName, string dataName,
            BHD5.Game formatVersion, bool bigEndian, long alignment, bool writeDataHeader, string dataHeaderVersion, int bucketDistribution, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var binder = FromDirectories(inputDirectory, hashedNamesDirectory);
            binder.Version = dataHeaderVersion;
            var countStrategy = new DistributionBucketCountStrategy(bucketDistribution, binder.Files.Count);
            var indexStrategy = new ModulusBucketIndexStrategy(countStrategy.ComputeBucketCount());
            binder.BucketInfo = new BucketInfo(countStrategy, indexStrategy);
            await binder.WriteToBinderHeader5Async(outputDirectory, headerName, dataName, formatVersion, bigEndian, alignment, writeDataHeader, progress, cancellationToken);
        }

        /// <summary>
        /// Write files found from the given information to a binder header 5 archive asynchronously.
        /// </summary>
        /// <param name="inputDirectory">The directory to get files from.</param>
        /// <param name="outputDirectory">The directory to output the archive to.</param>
        /// <param name="headerName">The name of the header file of the archive.</param>
        /// <param name="dataName">The name of the data file of the archive.</param>
        /// <param name="formatVersion">The version of the format of the archive.</param>
        /// <param name="bigEndian">Whether or not the archive is to be written in big endian.</param>
        /// <param name="alignment">The alignment to use for each file in the archive.</param>
        /// <param name="writeDataHeader">Whether or not to write a data header inside the data file.</param>
        /// <param name="dataHeaderVersion">The version string to set in the data header.</param>
        /// <param name="bucketDistribution">The average number of files per bucket.</param>
        /// <param name="progress">An object to report the progress of writing.</param>
        /// <param name="cancellationToken">An object to cancel operations when necessary.</param>
        /// <returns>A <see cref="Task"/> representing the work of writing.</returns>
        public async static Task WriteDirectoriesIntoBinderHeader5Async(string inputDirectory, string outputDirectory, string headerName, string dataName,
            BHD5.Game formatVersion, bool bigEndian, long alignment, bool writeDataHeader, string dataHeaderVersion, int bucketDistribution, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var binder = FromDirectory(inputDirectory);
            binder.Version = dataHeaderVersion;
            var countStrategy = new DistributionBucketCountStrategy(bucketDistribution, binder.Files.Count);
            var indexStrategy = new ModulusBucketIndexStrategy(countStrategy.ComputeBucketCount());
            binder.BucketInfo = new BucketInfo(countStrategy, indexStrategy);
            await binder.WriteToBinderHeader5Async(outputDirectory, headerName, dataName, formatVersion, bigEndian, alignment, writeDataHeader, progress, cancellationToken);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets a file name according to the pathing settings.
        /// </summary>
        /// <param name="path">A file path.</param>
        /// <returns>A file name.</returns>
        private string GetFileName(string path)
        {
            if (!string.IsNullOrWhiteSpace(RootDirectory))
            {
                return PathHandler.GetRelativePath(path, RootDirectory);
            }

            return path;
        }

        /// <summary>
        /// Gets a file name from a binder hash.
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <param name="hashDictionary">A hash dictionary possibly containing this hash.</param>
        /// <param name="nameIsHash">Whether or not the name was returned as using the hash itself.</param>
        /// <returns>A file name from a given binder hash.</returns>
        private static string GetFileNameFromBinderHash(ulong hash, BinderHashDictionary hashDictionary, out bool nameIsHash)
        {
            if (hashDictionary.TryGetValue(hash, out string? value))
            {
                nameIsHash = false;
                return value;
            }

            nameIsHash = true;
            return PathHandler.Combine("_unknown", hash.ToString());
        }

        /// <summary>
        /// Gets file length based on the given format version.
        /// </summary>
        /// <param name="fileHeader">A file header with length information.</param>
        /// <param name="formatVersion">The format version.</param>
        /// <returns>A file length.</returns>
        private static int GetFileLength(BHD5.FileHeader fileHeader, BHD5.Game formatVersion)
        {
            if (formatVersion >= BHD5.Game.DarkSouls3)
            {
                return (int)fileHeader.UnpaddedFileSize;
            }

            return fileHeader.PaddedFileSize;
        }

        /// <summary>
        /// Initializes the buckets in a <see cref="BHD5"/> to meet the given count.
        /// </summary>
        /// <param name="header">The header to initialize buckets in.</param>
        /// <param name="count">The number of buckets to initialize.</param>
        private static void InitializeBuckets(BHD5 header, int count)
        {
            header.Buckets = [];
            for (int i = 0; i < count; i++)
            {
                header.Buckets.Add([]);
            }
        }

        /// <summary>
        /// Gets bucket count based on the bucket info settings.
        /// <para>Used to create bucket info in case it was null.</para>
        /// </summary>
        /// <returns>A bucket count.</returns>
        private int GetBucketCount()
        {
            int bucketCount;
            if (BucketInfo == null)
            {
                var countStrategy = new DistributionBucketCountStrategy(7, Files.Count);
                bucketCount = countStrategy.ComputeBucketCount();
                var indexStrategy = new ModulusBucketIndexStrategy(bucketCount);
                BucketInfo = new BucketInfo(countStrategy, indexStrategy);
            }
            else
            {
                bucketCount = BucketInfo.BucketCountStrategy.ComputeBucketCount();
            }
            return bucketCount;
        }

        /// <summary>
        /// Gets a bucket index based on the bucket info settings.
        /// <para>Used to throw if bucket info is null at this point.</para>
        /// </summary>
        /// <param name="hash">The hash to get the bucket index of.</param>
        /// <returns>A bucket index.</returns>
        /// <exception cref="Exception">Bucket info was null where it shouldn't be.</exception>
        private int GetBucketIndex(ulong hash)
        {
            if (BucketInfo == null)
            {
                throw new Exception($"{nameof(BucketInfo)} should not be null.");
            }

            return BucketInfo.BucketIndexStrategy.ComputeBucketIndex(hash);
        }

        /// <summary>
        /// Gets a data header based on the data header settings.
        /// </summary>
        /// <param name="formatVersion">The format version of the archive header.</param>
        /// <param name="version">The data header version string.</param>
        /// <returns>A data header in bytes.</returns>
        private static byte[] GetDataHeader(BHD5.Game formatVersion, string version)
        {
            string magic = formatVersion >= BHD5.Game.DarkSouls2 && formatVersion <= BHD5.Game.EldenRing ? "BDF4" : "BDF3";
            byte[] magicBytes = Encoding.ASCII.GetBytes(magic);

            byte[] versionBytes;
            if (version.Length < 8)
            {
                versionBytes = new byte[8];
                byte[] smallVersionBytes = Encoding.ASCII.GetBytes(version);
                Array.Copy(smallVersionBytes, versionBytes, smallVersionBytes.Length);
            }
            else if (version.Length > 8)
            {
                versionBytes = Encoding.ASCII.GetBytes(version[0..8]);
            }
            else
            {
                versionBytes = Encoding.ASCII.GetBytes(version);
            }

            byte[] header = new byte[0x10];
            Array.Copy(magicBytes, 0, header, 0, 4);
            Array.Copy(versionBytes, 0, header, 4, 8);
            return header;
        }

        /// <summary>
        /// Gets a <see cref="BHD5"/> from the given path, decrypting if necessary.
        /// </summary>
        /// <param name="headerPath">The path to read the header from.</param>
        /// <param name="decryptionKey">A decryption key for decrypting the header.</param>
        /// <param name="formatVersion">The version of the format of the header.</param>
        /// <returns>The read header.</returns>
        internal static BHD5 ReadBHD5(string headerPath, string? decryptionKey, BHD5.Game formatVersion)
        {
            BHD5 header;
            if (!string.IsNullOrWhiteSpace(decryptionKey) && !BHD5.IsHeader(headerPath))
            {
                header = BHD5.Read(CryptographyHandler.DecryptRsa(headerPath, decryptionKey), formatVersion);
            }
            else
            {
                header = BHD5.Read(headerPath, formatVersion);
            }
            return header;
        }

        /// <summary>
        /// Pad a stream out to the given alignment.
        /// </summary>
        /// <param name="stream">The stream to pad.</param>
        /// <param name="alignment">The alignment to pad to.</param>
        private static void Pad(Stream stream, long alignment)
        {
            long remainder = stream.Position % alignment;
            long remaining = 0;
            if (remainder > 0)
            {
                remaining = alignment - remainder;
            }

            while (remaining > 0)
            {
                stream.WriteByte(0);
                remaining -= 1;
            }
        }

        /// <summary>
        /// Write data to the given path asynchronously.
        /// </summary>
        /// <param name="bytes">The data to write.</param>
        /// <param name="path">The path to write the data to.</param>
        /// <param name="cancellationToken">An object to cancel operations when necessary.</param>
        /// <returns>A <see cref="Task"/> holding the length of the written file.</returns>
        private async static Task<long> WriteToPathAsync(byte[] bytes, string path, CancellationToken cancellationToken)
        {
            string? directoryName = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            await fs.WriteAsync(bytes, cancellationToken);
            return bytes.Length;
        }

        #endregion

    }
}
