using BinderHandler.Hashes;
using BinderHandler.Progress;
using SoulsFormats;

namespace BinderHandler
{
    /// <summary>
    /// Unpacks a list of divided binder files.
    /// </summary>
    public class DivBinderUnpacker : List<DivBinderInfo>
    {
        /// <summary>
        /// Adds a single <see cref="Binder"/>.
        /// </summary>
        /// <param name="binder">The <see cref="Binder"/> to add.</param>
        /// <param name="dataPath">The path to the data file of this <see cref="Binder"/>.</param>
        public void Add(Binder binder, string dataPath)
            => Add(new DivBinderInfo(binder, dataPath));

        /// <summary>
        /// Adds a single <see cref="Binder"/> from the given pathing information.
        /// </summary>
        /// <param name="headerPath">The path to the header file.</param>
        /// <param name="dataPath">The path to the data file.</param>
        /// <param name="decryptionKey">The decryption key if necessary.</param>
        /// <param name="hashDictionary">A <see cref="BinderHashDictionary"/> possibly containing names for files.</param>
        /// <param name="formatVersion">The version of the format of the header.</param>
        public void AddFromPath(string headerPath, string dataPath, string? decryptionKey, BinderHashDictionary hashDictionary, BHD5.Game formatVersion)
            => Add(new DivBinderInfo(Binder.FromBinderHeader5(Binder.ReadBHD5(headerPath, decryptionKey, formatVersion), hashDictionary), dataPath));

        /// <summary>
        /// Set files not in the provided list to be ignored.
        /// </summary>
        /// <param name="selectedFiles">The selected list of files.</param>
        public void SetSelectedFiles(List<string> selectedFiles)
        {
            foreach (var binderinfo in this)
            {
                foreach (var file in binderinfo.Binder.Files)
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
        }

        /// <summary>
        /// Unpack the binders in the list.
        /// </summary>
        /// <param name="outputDirectory">The directory to output to.</param>
        public void UnpackBinders(string outputDirectory)
        {
            foreach (var binderinfo in this)
            {
                var binder = binderinfo.Binder;
                string dataPath = binderinfo.DataPath;

                if (binder.AllFilesIgnored()) continue;
                binder.UnpackDataFromPath(dataPath, outputDirectory);
            }
        }

        /// <summary>
        /// Unpack the binders in the list asynchronously.
        /// </summary>
        /// <param name="outputDirectory">The directory to output to.</param>
        /// <param name="progress">An object to report the progress of unpacking.</param>
        /// <param name="cancellationToken">An object to cancel operations when necessary.</param>
        public async Task UnpackBindersAsync(string outputDirectory, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var progresses = new List<IProgress<double>>(Count);
            var amalgamator = new ProgressAmalgamator(progress);
            
            for (int i = 0; i < Count; i++)
            {
                var currentProgress = new Progress<double>();
                progresses.Add(currentProgress);
                amalgamator.Attach(currentProgress);

                var binderinfo = this[i];
                var binder = binderinfo.Binder;
                string dataPath = binderinfo.DataPath;

                if (binder.AllFilesIgnored())
                {
                    progresses[i].Report(1);
                    continue;
                }

                await binder.UnpackDataFromPathAsync(dataPath, outputDirectory, progresses[i], cancellationToken);
            }
        }
    }
}
