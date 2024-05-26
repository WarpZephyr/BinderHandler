using BinderHandler.Handlers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BinderHandler.Hashes
{
    public class BinderHashDictionary
    {
        /// <summary>
        /// The prime for computing 32-bit hashes.
        /// </summary>
        public const uint BINDER_PATH_PRIME_32 = 37u;

        /// <summary>
        /// The prime for computing 64-bit hashes.
        /// </summary>
        public const ulong BINDER_PATH_PRIME_64 = 133ul;

        /// <summary>
        /// The underlying dictionary.
        /// </summary>
        private readonly Dictionary<ulong, string> _dictionary;

        /// <summary>
        /// Whether or not hashes are 64-bit.
        /// </summary>
        public bool Bit64 { get; set; }

        /// <summary>
        /// Create a <see cref="BinderHashDictionary"/>.
        /// </summary>
        public BinderHashDictionary()
        {
            _dictionary = [];
            Bit64 = false;
        }

        /// <summary>
        /// Create a <see cref="BinderHashDictionary"/> and set whether or not hashes are 64-bit.
        /// </summary>
        public BinderHashDictionary(bool bit64)
        {
            _dictionary = [];
            Bit64 = bit64;
        }

        /// <summary>
        /// Create a <see cref="BinderHashDictionary"/> with the given starting capacity.
        /// </summary>
        public BinderHashDictionary(int capacity)
        {
            _dictionary = new Dictionary<ulong, string>(capacity);
            Bit64 = false;
        }

        /// <summary>
        /// Create a <see cref="BinderHashDictionary"/> with the given starting capacity and set whether or not hashes are 64-bit.
        /// </summary>
        public BinderHashDictionary(int capacity, bool bit64)
        {
            _dictionary = new Dictionary<ulong, string>(capacity);
            Bit64 = bit64;
        }

        /// <summary>
        /// Create a <see cref="BinderHashDictionary"/> with the given existing dictionary.
        /// </summary>
        /// <param name="hashDictionary">An existing hash dictionary.</param>
        /// <param name="bit64">Whether or not hashes are 64-bit.</param>
        /// <exception cref="InvalidOperationException">A hash did not match it's paired value in the given dictionary.</exception>
        public BinderHashDictionary(Dictionary<ulong, string> hashDictionary, bool bit64)
        {
            foreach (var pair in hashDictionary)
            {
                if (ComputeHash(pair.Value, bit64) != pair.Key)
                {
                    throw new InvalidOperationException($"{nameof(pair)} hash {pair.Key} did not match {nameof(pair)} value {pair.Value}");
                }
            }

            _dictionary = hashDictionary;
        }

        /// <summary>
        /// Access a value from the dictionary by hash.
        /// </summary>
        /// <param name="hash">The hash of the value to find.</param>
        /// <returns>The value of the hash.</returns>
        public string this[ulong hash] => _dictionary[hash];

        /// <summary>
        /// Computes the hash of a file path string.
        /// </summary>
        /// <param name="value">The file path string to compute the hash of.</param>
        /// <returns>The hash of a file path string.</returns>
        public ulong ComputeHash(string value)
        {
            string hashable = value.Trim().Replace('\\', '/').ToLowerInvariant();
            if (!hashable.StartsWith('/'))
            {
                hashable = '/' + hashable;
            }

            return Bit64 ? hashable.Aggregate(0ul, (i, c) => i * BINDER_PATH_PRIME_64 + c) : hashable.Aggregate(0u, (i, c) => i * BINDER_PATH_PRIME_32 + c);
        }

        /// <summary>
        /// Computes the hash of a file path string.
        /// </summary>
        /// <param name="value">The file path string to compute the hash of.</param>
        /// <param name="bit64">Whether or not hashes are 64-bit.</param>
        /// <returns>The hash of a file path string.</returns>
        public static ulong ComputeHash(string value, bool bit64)
        {
            string hashable = value.Trim().Replace('\\', '/').ToLowerInvariant();
            if (!hashable.StartsWith('/'))
            {
                hashable = '/' + hashable;
            }

            return bit64 ? hashable.Aggregate(0ul, (i, c) => i * BINDER_PATH_PRIME_64 + c) : hashable.Aggregate(0u, (i, c) => i * BINDER_PATH_PRIME_32 + c);
        }

        /// <summary>
        /// Checks whether or not two values' hashes are the same.
        /// </summary>
        /// <param name="valueA">The first value.</param>
        /// <param name="valueB">The second value.</param>
        /// <returns>Whether or not these two values' hashes collide.</returns>
        public bool Collides(string valueA, string valueB)
        {
            if (valueA.Equals(valueB))
            {
                return true;
            }

            return ComputeHash(valueA, Bit64) == ComputeHash(valueB, Bit64);
        }

        /// <summary>
        /// Checks whether or not two values' hashes are the same.
        /// </summary>
        /// <param name="valueA">The first value.</param>
        /// <param name="valueB">The second value.</param>
        /// <param name="bit64">Whether or not hashes are 64-bit.</param>
        /// <returns>Whether or not these two values' hashes collide.</returns>
        public static bool Collides(string valueA, string valueB, bool bit64)
        {
            if (valueA.Equals(valueB))
            {
                return true;
            }

            return ComputeHash(valueA, bit64) == ComputeHash(valueB, bit64);
        }

        /// <summary>
        /// Gets a list of values that the dictionary actually contains from a list of values.
        /// </summary>
        /// <param name="values">A list of values.</param>
        /// <returns>A list of values the dictionary actually contains.</returns>
        public List<string> GetExistingValues(List<string> values)
        {
            var existingValues = new List<string>();
            foreach (string value in values)
            {
                if (_dictionary.ContainsKey(ComputeHash(value)))
                {
                    existingValues.Add(value);
                }
            }
            return existingValues;
        }

        #region Factory Methods

        /// <summary>
        /// Creates a <see cref="BinderHashDictionary"/> from the provided strings in a file at the specified path.
        /// </summary>
        /// <param name="path">The path to a file.</param>
        /// <param name="bit64">Whether or not hashes should be 64-bit.</param>
        /// <returns>A <see cref="BinderHashDictionary"/>.</returns>
        public static BinderHashDictionary FromPath(string path, bool bit64)
        {
            PathExceptionHandler.ThrowIfNotFile(path, nameof(path));
            string[] fileNames = File.ReadAllLines(path);
            var hashDictionary = new BinderHashDictionary(fileNames.Length, bit64);
            hashDictionary.AddRange(fileNames);
            return hashDictionary;
        }

        /// <summary>
        /// Creates a <see cref="BinderHashDictionary"/> from the provided strings.
        /// </summary>
        /// <param name="lines">The lines to read into a hash dictionary.</param>
        /// <param name="bit64">Whether or not hashes should be 64-bit.</param>
        /// <returns>A <see cref="BinderHashDictionary"/>.</returns>
        public static BinderHashDictionary FromLines(List<string> lines, bool bit64)
        {
            var hashDictionary = new BinderHashDictionary(lines.Count, bit64);
            hashDictionary.AddRange(lines);
            return hashDictionary;
        }

        /// <summary>
        /// Reads a set of lines that have several dictionaries split by terminator lines of some kind.
        /// </summary>
        /// <param name="multiDictionaryPath">A path to a file containing the lines to read dictionaries out of.</param>
        /// <param name="bit64">Whether or not each dictionary's hashes are 64-bit.</param>
        /// <param name="terminatorLineStart">The start of each terminator line.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="BinderHashDictionary"/>.</returns>
        public static List<BinderHashDictionary> FromMultiDictionaryPath(string multiDictionaryPath, bool bit64, string terminatorLineStart = "#")
        {
            PathExceptionHandler.ThrowIfNotFile(multiDictionaryPath);
            return FromMultiDictionaryLines(File.ReadAllLines(multiDictionaryPath), bit64, terminatorLineStart);
        }

        /// <summary>
        /// Reads a set of lines that have several dictionaries split by terminator lines of some kind.
        /// </summary>
        /// <param name="rawLines">The lines to read dictionaries out of.</param>
        /// <param name="bit64">Whether or not each dictionary's hashes are 64-bit.</param>
        /// <param name="terminatorLineStart">The start of each terminator line.</param>
        /// <returns>A list of dictionaries.</returns>
        public static List<BinderHashDictionary> FromMultiDictionaryLines(IList<string> rawLines, bool bit64, string terminatorLineStart = "#")
        {
            var dictionaries = new List<BinderHashDictionary>();
            int dictionariesIndex = -1;
            foreach (string line in rawLines)
            {
                if (line.StartsWith(terminatorLineStart))
                {
                    dictionariesIndex += 1;
                    dictionaries.Add(new BinderHashDictionary(bit64));
                    continue;
                }
                else if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                dictionaries[dictionariesIndex].Add(line);
            }

            return dictionaries;
        }

        /// <summary>
        /// Reads a list of dictionaries from a list of paths.
        /// </summary>
        /// <param name="paths">A list of file paths containing dictionaries.</param>
        /// <param name="bit64">Whether or not the hashes in each dictionary are 64-bit.</param>
        /// <returns>A list of dictionaries.</returns>
        public static List<BinderHashDictionary> FromPaths(List<string> paths, bool bit64)
        {
            var dictionaries = new List<BinderHashDictionary>(paths.Count);
            foreach (string path in paths)
            {
                PathExceptionHandler.ThrowIfNotFile(path, nameof(path));
                dictionaries.Add(FromPath(path, bit64));
            }
            return dictionaries;
        }

        #endregion

        #region Dictionary Implementation

        /// <summary>
        /// Adds a value to the dictionary.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <exception cref="HashCollisionException">A hash collision occurred.</exception>
        /// <exception cref="DuplicateValueException">A given value already exists in the dictionary.</exception>
        public void Add(string value)
        {
            var hash = ComputeHash(value, Bit64);
            if (!_dictionary.TryAdd(hash, value))
            {
                var originalValue = _dictionary[hash];
                if (originalValue != value)
                {
                    throw new HashCollisionException($"A hash collision has been detected for two different values: Hash: {hash}; Values: {originalValue}; {value}");
                }

                throw new DuplicateValueException($"Value has already been added: Hash: {hash}; Value: {value}");
            }
        }

        /// <summary>
        /// Removes the value with the specified hash.
        /// </summary>
        /// <param name="hash">The hash to remove.</param>
        /// <returns>Returns <see langword="true" /> if the element is successfully found and removed; otherwise, <see langword="false" />.  This method returns <see langword="false" /> if <paramref name="hash" /> is not found in the <see cref="BinderHashDictionary" />.</returns>
        public bool Remove(ulong hash)
        {
            return _dictionary.Remove(hash);
        }

        /// <summary>
        /// Removes the specified value by searching the dictionary with it's computed hash.
        /// </summary>
        /// <param name="value">The value to remove.</param>
        /// <returns>Returns <see langword="true" /> if the element is successfully found and removed; otherwise, <see langword="false" />.  This method returns <see langword="false" /> if <paramref name="value" /> is not found in the <see cref="BinderHashDictionary" />.</returns>
        public bool Remove(string value)
        {
            return _dictionary.Remove(ComputeHash(value, Bit64));
        }

        /// <summary>
        /// Attempts to add the specified value to the <see cref="BinderHashDictionary"/>.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>Whether or not adding the value was successful.</returns>
        public bool TryAdd(string value)
        {
            return _dictionary.TryAdd(ComputeHash(value, Bit64), value);
        }

        /// <summary>
        /// Attempts to add the specified value to the <see cref="BinderHashDictionary"/>.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <param name="hash">The hash generated from the attempt.</param>
        /// <returns>Whether or not adding the value was successful.</returns>
        public bool TryAdd(string value, out ulong hash)
        {
            hash = ComputeHash(value, Bit64);
            return _dictionary.TryAdd(hash, value);
        }

        /// <summary>
        /// Attempts to get the value associated with the specified hash.
        /// </summary>
        /// <param name="hash">The hash to attempt to get the value of.</param>
        /// <param name="value">The value if found. <see langword="null"/> if not found.</param>
        /// <returns>Returns <see langword="true"/> if found, and <see langword="false"/> otherwise.</returns>
        public bool TryGetValue(ulong hash, [NotNullWhen(true)] out string? value)
        {
            return _dictionary.TryGetValue(hash, out value);
        }

        /// <summary>
        /// Add a range of values to the <see cref="BinderHashDictionary"/>.
        /// </summary>
        /// <param name="values">The values to add.</param>
        public void AddRange(string[] values)
        {
            foreach (var value in values)
            {
                Add(value);
            }
        }

        /// <summary>
        /// Add a range of values to the <see cref="BinderHashDictionary"/>.
        /// </summary>
        /// <param name="values">The values to add.</param>
        public void AddRange(IEnumerable<string> values)
        {
            foreach (var value in values)
            {
                Add(value);
            }
        }

        /// <summary>
        /// Add a range of values to the <see cref="BinderHashDictionary"/>.
        /// </summary>
        /// <param name="values">The values to add.</param>
        public void AddRange(ReadOnlySpan<string> values)
        {
            foreach (var value in values)
            {
                Add(value);
            }
        }

        /// <summary>
        /// Gets a list containing the hashes in the <see cref="BinderHashDictionary"/>.
        /// </summary>
        /// <returns>A <see cref="List{ulong}" /> of hashes.</returns>
        public List<ulong> GetHashes()
        {
            return [.. _dictionary.Keys];
        }

        /// <summary>
        /// Gets a list containing the values in the <see cref="BinderHashDictionary"/>.
        /// </summary>
        /// <returns>A <see cref="List{string}" /> of values.</returns>
        public List<string> GetValues()
        {
            return [.._dictionary.Values];
        }

        /// <summary>
        /// Determines whether or not the <see cref="BinderHashDictionary"/> contains the specified hash.
        /// </summary>
        /// <param name="hash">The hash to check for.</param>
        /// <returns>Returns <see langword="true" /> if the <see cref="BinderHashDictionary"/> contains an element with the specified hash; otherwise, <see langword="false" />.</returns>
        public bool ContainsHash(ulong hash)
        {
            return _dictionary.ContainsKey(hash);
        }

        /// <summary>
        /// Determines whether or not the <see cref="BinderHashDictionary"/> contains the specified value.
        /// </summary>
        /// <param name="value">The value to check for.</param>
        /// <returns>Returns <see langword="true" /> if the <see cref="BinderHashDictionary"/> contains an element with the specified value; otherwise, <see langword="false" />.</returns>
        public bool ContainsValue(string value)
        {
            return _dictionary.ContainsKey(ComputeHash(value, Bit64));
        }

        /// <summary>
        /// Determines whether or not the dictionary contains all of the given hashes.
        /// </summary>
        /// <param name="hashes">The hashes to check.</param>
        /// <returns>Returns <see langword="true" /> if the <see cref="BinderHashDictionary"/> contains all specified hashes; otherwise, <see langword="false" />.</returns>
        public bool ContainsHashes(List<ulong> hashes)
        {
            foreach (ulong hash in hashes)
            {
                if (!_dictionary.ContainsKey(hash))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Determines whether or not the dictionary contains all of the given values.
        /// </summary>
        /// <param name="values">The values to check.</param>
        /// <returns>Returns <see langword="true" /> if the <see cref="BinderHashDictionary"/> contains all specified values; otherwise, <see langword="false" />.</returns>
        public bool ContainsValues(List<string> values)
        {
            foreach (string value in values)
            {
                if (!_dictionary.ContainsKey(ComputeHash(value)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Removes all hashes and values from the <see cref="BinderHashDictionary"/>.
        /// </summary>
        public void Clear() => _dictionary.Clear();

        #endregion
    }
}
