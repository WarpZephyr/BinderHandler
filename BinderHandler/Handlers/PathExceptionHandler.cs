using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace BinderHandler.Handlers
{
    internal static class PathExceptionHandler
    {
        internal static void ThrowIfNotFile([NotNull] string? filePath, [CallerArgumentExpression(nameof(filePath))] string? paramName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath, paramName);
            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"A file did not exist at: {filePath}", paramName);
            }
        }

        internal static void ThrowIfNotDirectory([NotNull] string? directoryPath, [CallerArgumentExpression(nameof(directoryPath))] string? paramName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath, paramName);
            if (!Directory.Exists(directoryPath))
            {
                throw new ArgumentException($"A directory did not exist at: {directoryPath}", paramName);
            }
        }

        internal static void ThrowIfNotFileOrDirectory([NotNull] string? path, [CallerArgumentExpression(nameof(path))] string? paramName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path, paramName);
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                throw new ArgumentException($"Neither a file or directory exists at: {path}", paramName);
            }
        }

        internal static void ThrowIfFile([NotNull] string? path, [CallerArgumentExpression(nameof(path))] string? paramName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path, paramName);
            if (File.Exists(path))
            {
                throw new ArgumentException($"Must not be file at: {path}", paramName);
            }
        }

        internal static void ThrowIfDirectory([NotNull] string? path, [CallerArgumentExpression(nameof(path))] string? paramName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path, paramName);
            if (Directory.Exists(path))
            {
                throw new ArgumentException($"Must not be directory at: {path}", paramName);
            }
        }

        internal static void ThrowIfFileOrDirectory([NotNull] string? path, [CallerArgumentExpression(nameof(path))] string? paramName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path, paramName);
            if (File.Exists(path) || Directory.Exists(path))
            {
                throw new ArgumentException($"Must not be file or directory at: {path}", paramName);
            }
        }

        internal static void ThrowIfNullOrFile([NotNull] string? path, [CallerArgumentExpression(nameof(path))] string? paramName = null)
        {
            ArgumentNullException.ThrowIfNull(path, paramName);
            ThrowIfFile(path, paramName);
        }

        internal static void ThrowIfNullOrDirectory([NotNull] string? path, [CallerArgumentExpression(nameof(path))] string? paramName = null)
        {
            ArgumentNullException.ThrowIfNull(path, paramName);
            ThrowIfDirectory(path, paramName);
        }

        internal static void ThrowIfNullFileOrDirectory([NotNull] string? path, [CallerArgumentExpression(nameof(path))] string? paramName = null)
        {
            ArgumentNullException.ThrowIfNull(path, paramName);
            ThrowIfFileOrDirectory(path, paramName);
        }

        internal static void ThrowIfNullWhiteSpaceOrFile([NotNull] string? path, [CallerArgumentExpression(nameof(path))] string? paramName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path, paramName);
            ThrowIfFile(path, paramName);
        }

        internal static void ThrowIfNullWhiteSpaceOrDirectory([NotNull] string? path, [CallerArgumentExpression(nameof(path))] string? paramName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path, paramName);
            ThrowIfDirectory(path, paramName);
        }

        internal static void ThrowIfNullWhiteSpaceFileOrDirectory([NotNull] string? path, [CallerArgumentExpression(nameof(path))] string? paramName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path, paramName);
            ThrowIfFileOrDirectory(path, paramName);
        }

        internal static void ThrowIfRooted([NotNull] string? path, [CallerArgumentExpression(nameof(path))] string? paramName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path, paramName);
            if (Path.IsPathRooted(path))
            {
                throw new ArgumentException($"The provided path should not have a root: {path}", paramName);
            }
        }

        internal static void ThrowIfNullOrRooted([NotNull] string? path, [CallerArgumentExpression(nameof(path))] string? paramName = null)
        {
            ArgumentNullException.ThrowIfNull(path, paramName);
            ThrowIfRooted(path, paramName);
        }

        internal static void ThrowIfNullWhiteSpaceOrRooted([NotNull] string? path, [CallerArgumentExpression(nameof(path))] string? paramName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path, paramName);
            ThrowIfRooted(path, paramName);
        }
    }
}
