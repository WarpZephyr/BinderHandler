namespace BinderHandler.Progress
{
    // Modified from: https://stackoverflow.com/a/75890394
    internal sealed class ProgressAmalgamator(IProgress<double> progress)
    {
        internal IProgress<double> Progress { get; set; } = progress;
        readonly List<IProgress<double>> _progressors = [];
        readonly List<double> _progress = [];
        readonly object _lock = new();

        internal void Attach(Progress<double> progress)
        {
            lock (_lock)
            {
                _progressors.Add(progress);
                _progress.Add(0);

                progress.ProgressChanged += Progress_ProgressChanged;
            }
        }

        void Progress_ProgressChanged(object? sender, double e)
        {
            double average = 0;

            lock (_lock)
            {
                for (int i = 0; i < _progressors.Count; i++)
                {
                    if (ReferenceEquals(_progressors[i], sender))
                    {
                        _progress[i] = e;
                        break;
                    }
                }

                average = _progress.Average();
            }

            Progress.Report(average);
        }
    }
}
