using BinderHandler.Handlers;
using SoulsFormats;
using System.Text;
using System.Text.RegularExpressions;

namespace BinderHandler.Guessing
{
    /// <summary>
    /// An extension guesser for many FromSoftware file formats.
    /// </summary>
    public static partial class ExtensionGuesser
    {
        /// <summary>
        /// Guess the extensions of all files in a directory, renaming them to use that extension afterwards.
        /// </summary>
        /// <param name="directory">The directory to guess the extensions of each file in.</param>
        /// <param name="recursive">Whether or not to search all directories or just the top directory.</param>
        public static void GuessExtensions(string directory, bool recursive = false)
        {
            PathExceptionHandler.ThrowIfNotDirectory(directory, nameof(directory));
            var files = Directory.EnumerateFiles(directory, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            foreach (var path in files)
            {
                string newPath = path + GuessExtension(path);
                if (!File.Exists(newPath))
                {
                    File.Move(path, newPath);
                }
            }
        }

        /// <summary>
        /// Guess the extension of a file. 
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>The guessed extension.</returns>
        public static string GuessExtension(string path)
        {
            PathExceptionHandler.ThrowIfNotFile(path, nameof(path));
            using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return GuessExtension(fs);
        }

        /// <summary>
        /// Guess the extension of a file asynchronously. 
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>The guessed extension.</returns>
        public async static Task<string> GuessExtensionAsync(string path)
        {
            PathExceptionHandler.ThrowIfNotFile(path, nameof(path));
            using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            string extension = await GuessExtensionAsync(fs);
            return extension;
        }

        /// <summary>
        /// Guess the extension of the specified data stream. 
        /// </summary>
        /// <param name="stream">A data stream.</param>
        /// <returns>The guessed extension.</returns>
        public static string GuessExtension(Stream stream)
        {
            long pos = stream.Position;
            int signatureCheckLength = 50;
            byte[] signatureBytes = new byte[(stream.Length - stream.Position) < signatureCheckLength ? stream.Length : signatureCheckLength];
            stream.ReadExactly(signatureBytes);
            stream.Position = pos;
            string signature = Encoding.UTF8.GetString(signatureBytes);

            if (signature.StartsWith("BND")) return ".bnd";
            if (signature.StartsWith("BHD")) return ".bhd";
            if (signature.StartsWith("BHF")) return ".bhd";
            if (signature.StartsWith("BDF")) return ".bdt";
            if (signature.StartsWith("SMD")) return ".smd";
            if (signature.StartsWith("MDL")) return ".mdl";
            if (signature.StartsWith("FEV")) return ".fev";
            if (signature.StartsWith("FSB")) return ".fsb";
            if (signature.StartsWith("GFX")) return ".gfx";
            if (signature.StartsWith("PAM")) return ".pam";
            if (signature.StartsWith("CLM")) return ".clm";
            if (signature.StartsWith("TPF\0")) return ".tpf";
            if (signature.StartsWith("MQB ")) return ".mqb";
            if (signature.StartsWith("TAE ")) return ".tae";
            if (signature.StartsWith("DRB\0")) return ".drb";
            if (signature.StartsWith("\0BRD")) return ".drb";
            if (signature.StartsWith("DDS ")) return ".dds";
            if (signature.StartsWith("ENFL")) return ".entryfilelist";
            if (signature.StartsWith("DFPN")) return ".nfd";
            if (signature.StartsWith("#BOM")) return ".txt";
            if (signature.StartsWith("TEXT")) return ".txt";
            if (signature.StartsWith("NVMA")) return ".nva";
            if (signature.StartsWith("HNAV")) return ".hnav";
            if (signature.StartsWith("NVG2")) return ".ngp";
            if (signature.StartsWith("F2TR")) return ".flver2tri";
            if (signature.StartsWith("EDF\0")) return ".edf";
            if (signature.StartsWith("EVD\0")) return ".evd";
            if (signature.StartsWith("ELD\0")) return ".eld";
            if (signature.StartsWith("BLF\0")) return ".blf";
            if (signature.StartsWith("FXR\0")) return ".fxr";
            if (signature.StartsWith("ACB\0")) return ".acb";
            if (signature.StartsWith("HTR\0")) return ".ht";
            if (signature.StartsWith("ANE\0")) return ".ane";
            if (signature.StartsWith("<?xml")) return ".xml";
            if (signature.StartsWith("FLVER\0")) return ".flver";
            if (signature.StartsWith("[PATH]")) return ".ini";
            if (signature.StartsWith("-----BEGIN RSA PUBLIC KEY-----")) return ".pem";
            if (signature.StartsWith("DLSE", StringComparison.InvariantCultureIgnoreCase)) return ".ffx";
            if (signature.StartsWith("FSSL", StringComparison.InvariantCultureIgnoreCase)) return ".esd";
            if (signature.Length >= 4 && signature[1..].StartsWith("PNG")) return ".png";
            if (signature.Length >= 4 && signature[1..].StartsWith("Lua")) return ".lc";
            if (signature.Length >= 16 && signature[8..].StartsWith("FEV FMT ")) return ".fev";
            if (signature.Length >= 0x1A && signature[0xC..].StartsWith("ITLIMITER_INFO")) return ".itl";
            if (signature.Length >= 0x28 && signature[0x20..].StartsWith("#ANIEDIT")) return ".anc";
            if (signature.Length >= 0x2C && signature[0x28..].StartsWith("SIB ")) return ".sib";
            if (signature.Length >= 0x30 && signature[0x2C..].StartsWith("MTD ")) return ".mtd";
            if (FMG.TryRead(stream, out _)) return ".fmg";
            if (PARAM.TryRead(stream, out _)) return ".param";
            if (PARAMDEF.TryRead(stream, out _)) return ".paramdef";
            if (PARAMDBP.TryRead(stream, out _)) return ".dbp";
            if (CheckMSB(stream)) return ".msb";
            if (CheckTDF(stream)) return ".tdf";
            if (DCX.Is(stream)) return GuessExtension(DCX.DecompressToStream(stream)) + ".dcx";

            return string.Empty;
        }

        /// <summary>
        /// Guess the extension of the specified data stream asynchronously. 
        /// </summary>
        /// <param name="stream">A data stream.</param>
        /// <returns>The guessed extension.</returns>
        public async static Task<string> GuessExtensionAsync(Stream stream)
        {
            long pos = stream.Position;
            int signatureCheckLength = 50;
            byte[] signatureBytes = new byte[(stream.Length - stream.Position) < signatureCheckLength ? stream.Length : signatureCheckLength];
            await stream.ReadExactlyAsync(signatureBytes);
            stream.Position = pos;
            string signature = Encoding.UTF8.GetString(signatureBytes);

            if (signature.StartsWith("BND")) return ".bnd";
            if (signature.StartsWith("BHD")) return ".bhd";
            if (signature.StartsWith("BHF")) return ".bhd";
            if (signature.StartsWith("BDF")) return ".bdt";
            if (signature.StartsWith("SMD")) return ".smd";
            if (signature.StartsWith("MDL")) return ".mdl";
            if (signature.StartsWith("FEV")) return ".fev";
            if (signature.StartsWith("FSB")) return ".fsb";
            if (signature.StartsWith("GFX")) return ".gfx";
            if (signature.StartsWith("PAM")) return ".pam";
            if (signature.StartsWith("CLM")) return ".clm";
            if (signature.StartsWith("TPF\0")) return ".tpf";
            if (signature.StartsWith("MQB ")) return ".mqb";
            if (signature.StartsWith("TAE ")) return ".tae";
            if (signature.StartsWith("DRB\0")) return ".drb";
            if (signature.StartsWith("\0BRD")) return ".drb";
            if (signature.StartsWith("DDS ")) return ".dds";
            if (signature.StartsWith("ENFL")) return ".entryfilelist";
            if (signature.StartsWith("DFPN")) return ".nfd";
            if (signature.StartsWith("#BOM")) return ".txt";
            if (signature.StartsWith("TEXT")) return ".txt";
            if (signature.StartsWith("NVMA")) return ".nva";
            if (signature.StartsWith("HNAV")) return ".hnav";
            if (signature.StartsWith("NVG2")) return ".ngp";
            if (signature.StartsWith("F2TR")) return ".flver2tri";
            if (signature.StartsWith("EDF\0")) return ".edf";
            if (signature.StartsWith("EVD\0")) return ".evd";
            if (signature.StartsWith("ELD\0")) return ".eld";
            if (signature.StartsWith("BLF\0")) return ".blf";
            if (signature.StartsWith("FXR\0")) return ".fxr";
            if (signature.StartsWith("ACB\0")) return ".acb";
            if (signature.StartsWith("HTR\0")) return ".ht";
            if (signature.StartsWith("ANE\0")) return ".ane";
            if (signature.StartsWith("<?xml")) return ".xml";
            if (signature.StartsWith("FLVER\0")) return ".flver";
            if (signature.StartsWith("[PATH]")) return ".ini";
            if (signature.StartsWith("-----BEGIN RSA PUBLIC KEY-----")) return ".pem";
            if (signature.StartsWith("DLSE", StringComparison.InvariantCultureIgnoreCase)) return ".ffx";
            if (signature.StartsWith("FSSL", StringComparison.InvariantCultureIgnoreCase)) return ".esd";
            if (signature.Length >= 4 && signature[1..].StartsWith("PNG")) return ".png";
            if (signature.Length >= 4 && signature[1..].StartsWith("Lua")) return ".lc";
            if (signature.Length >= 16 && signature[8..].StartsWith("FEV FMT ")) return ".fev";
            if (signature.Length >= 0x1A && signature[0xC..].StartsWith("ITLIMITER_INFO")) return ".itl";
            if (signature.Length >= 0x28 && signature[0x20..].StartsWith("#ANIEDIT")) return ".anc";
            if (signature.Length >= 0x2C && signature[0x28..].StartsWith("SIB ")) return ".sib";
            if (signature.Length >= 0x30 && signature[0x2C..].StartsWith("MTD ")) return ".mtd";
            if (CheckParam(stream)) return ".param";
            if (CheckMSB(stream)) return ".msb";
            if (CheckTDF(stream)) return ".tdf";
            if (DCX.Is(stream)) return GuessExtension(DCX.DecompressToStream(stream)) + ".dcx";
            if (FMG.TryRead(stream, out _)) return ".fmg";
            if (PARAMDEF.TryRead(stream, out _)) return ".paramdef";
            if (PARAMDBP.TryRead(stream, out _)) return ".dbp";

            return string.Empty;
        }

        /// <summary>
        /// Check if the given stream may be a param.
        /// </summary>
        /// <param name="stream">A stream.</param>
        /// <returns>Whether or not the given stream is believed to be a param.</returns>
        private static bool CheckParam(Stream stream)
        {
            using var br = new BinaryReaderEx(false, stream, true);
            if (br.Length < 0x2C)
            {
                return false;
            }

            string param = br.GetASCII(0xC, 0x20);
            return ParamRegex().IsMatch(param);
        }

        [GeneratedRegex("^[^\0]+\0 *$")]
        private static partial Regex ParamRegex();

        /// <summary>
        /// Check if the given stream may be an MSB.
        /// </summary>
        /// <param name="stream">A stream.</param>
        /// <returns>Whether or not the given stream is believed to be an MSB.</returns>
        private static bool CheckMSB(Stream stream)
        {
            using var br = new BinaryReaderEx(false, stream, true);
            if (br.Length < 8)
            {
                return false;
            }

            int offset = br.GetInt32(br.Position + 4);
            if (offset > br.Length || offset < 0)
            {
                byte[] offsetbytes = BitConverter.GetBytes(offset);
                Array.Reverse(offsetbytes);
                offset = BitConverter.ToInt32(offsetbytes, 0);
            }

            if (offset < 0 || offset >= br.Length - 1)
            {
                return false;
            }

            try
            {
                return br.GetASCII(br.Position + offset) == "MODEL_PARAM_ST";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if the given stream may be an TDF.
        /// </summary>
        /// <param name="stream">A stream.</param>
        /// <returns>Whether or not the given stream is believed to be a TDF.</returns>
        private static bool CheckTDF(Stream stream)
        {
            long pos = stream.Position;
            using var br = new BinaryReader(stream, Encoding.GetEncoding("shift-jis"), true);
            
            if (br.BaseStream.Length < 4 || br.ReadChar() != '\"')
            {
                stream.Position = pos;
                return false;
            }

            for (int i = 1; i < br.BaseStream.Length; i++)
            {
                if (br.ReadChar() == '\"' && i < br.BaseStream.Length - 2)
                {
                    char cr = br.ReadChar();
                    char lf = br.ReadChar();
                    stream.Position = pos;
                    return cr == '\r' && lf == '\n';
                }
            }

            stream.Position = pos;
            return false;
        }
    }
}
