/*
TransparentEmbed - Tools to embed and extract data into and from transparent pixels
Written in 2023 by VDZ
To the extent possible under law, the author(s) have dedicated all copyright and related and neighboring rights to this software to the public domain worldwide. This software is distributed without any warranty.
You should have received a copy of the CC0 Public Domain Dedication along with this software. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.
*/
//TL;DR for above notice: You can do whatever you want with this including commercial use without any restrictions or requirements.

using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;

namespace TransparentExtract
{
    class ArgumentParser
    {
        string[] args = null;
        int pos = 0;
        public ArgumentParser(string[] args) { this.args = args; }
        public bool HasNext() { return pos < args.Length; }
        public string GetNext() { return ((args != null && HasNext()) ? args[pos++] : null); }
    }

    public class EmbeddedData
    {
        public enum ContentType
        {
            Invalid = 0x00,
            Data = 0x01,
            File = 0x02,
            Text = 0x03
        }
        
        public ContentType contentType = ContentType.Invalid;
        public bool hashMatches = false;
        public byte[] data = null;
    }

    public class TransparentExtractor
    {
        public const byte VERSION_FORMAT = 0x01;
        public const string KEY_DEFAULT = "SecureBeneathTheWatchfulEyes";
        public static byte[] salt =
        {
            0x41, 0x72, 0x67, 0x75, 0x69, 0x6e, 0x67, 0x20, 0x74, 0x68, 0x61, 0x74, 0x20, 0x79, 0x6f, 0x75,
            0x20, 0x64, 0x6f, 0x6e, 0x27, 0x74, 0x20, 0x63, 0x61, 0x72, 0x65, 0x20, 0x61, 0x62, 0x6f, 0x75,
            0x74, 0x20, 0x74, 0x68, 0x65, 0x20, 0x72, 0x69, 0x67, 0x68, 0x74, 0x20, 0x74, 0x6f, 0x20, 0x70,
            0x72, 0x69, 0x76, 0x61, 0x63, 0x79, 0x20, 0x62, 0x65, 0x63, 0x61, 0x75, 0x73, 0x65, 0x20, 0x79,
            0x6f, 0x75, 0x20, 0x68, 0x61, 0x76, 0x65, 0x20, 0x6e, 0x6f, 0x74, 0x68, 0x69, 0x6e, 0x67, 0x20,
            0x74, 0x6f, 0x20, 0x68, 0x69, 0x64, 0x65, 0x20, 0x69, 0x73, 0x20, 0x6e, 0x6f, 0x20, 0x64, 0x69,
            0x66, 0x66, 0x65, 0x72, 0x65, 0x6e, 0x74, 0x20, 0x74, 0x68, 0x61, 0x6e, 0x20, 0x73, 0x61, 0x79,
            0x69, 0x6e, 0x67, 0x20, 0x79, 0x6f, 0x75, 0x20, 0x64, 0x6f, 0x6e, 0x27, 0x74, 0x20, 0x63, 0x61,
            0x72, 0x65, 0x20, 0x61, 0x62, 0x6f, 0x75, 0x74, 0x20, 0x66, 0x72, 0x65, 0x65, 0x20, 0x73, 0x70,
            0x65, 0x65, 0x63, 0x68, 0x20, 0x62, 0x65, 0x63, 0x61, 0x75, 0x73, 0x65, 0x20, 0x79, 0x6f, 0x75,
            0x20, 0x68, 0x61, 0x76, 0x65, 0x20, 0x6e, 0x6f, 0x74, 0x68, 0x69, 0x6e, 0x67, 0x20, 0x74, 0x6f,
            0x20, 0x73, 0x61, 0x79, 0x2e
        };
        private static bool silent = true;
        private static bool verbose = false;

        public static void Main(string[] args)
        {
            silent = false;

            //Read arguments
            ArgumentParser ap = new ArgumentParser(args);
            string key = KEY_DEFAULT;
            int threshold = 0;
            bool doPrint = false;
            bool inputIsList = false;
            string inputfile = null;
            string outputfile = null;
            List<string> appendFiles = new List<string>();
            while (ap.HasNext())
            {
                string arg = ap.GetNext();
                if (arg.StartsWith("-"))
                {
                    switch (arg)
                    {
                        case "-k":
                        case "--key":
                            if (!ap.HasNext())
                            {
                                Console.WriteLine("No argument provided for -k or --key!");
                                return;
                            }
                            key = ap.GetNext();
                            break;
                        case "-t":
                        case "--threshold":
                            try
                            {
                                threshold = Convert.ToInt32(ap.GetNext());
                                if (threshold < 0) throw new Exception("Threshold must be 0 or higher!");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Could not read value for threshold: " + ex.ToString());
                                return;
                            }
                            break;
                        case "-l":
                        case "--list":
                            inputIsList = true;
                            break;
                        case "-a":
                        case "--append":
                            if (!ap.HasNext())
                            {
                                Console.WriteLine("No argument provided for -a or --appendfile!");
                                return;
                            }
                            appendFiles.Add(ap.GetNext());
                            break;
                        case "-p":
                        case "--print":
                            doPrint = true;
                            break;
                        case "-v":
                        case "--verbose":
                            verbose = true;
                            break;
                        case "-s":
                        case "--silent":
                            silent = true;
                            break;
                        default:
                            Console.WriteLine("Unrecognized option: " + arg);
                            PrintUsage();
                            return;
                    }
                }
                else
                {
                    if (inputfile == null) { inputfile = arg; }
                    else if (outputfile == null) { outputfile = arg; }
                    else
                    {
                        Console.WriteLine("Too many arguments specified.");
                        PrintUsage();
                        return;
                    }
                }
            }

            //Check required arguments
            if (inputfile == null)
            {
                Console.WriteLine("No input file specified!");
                PrintUsage();
                return;
            }
            if (outputfile == null && !doPrint)
            {
                Console.WriteLine("No output file specified!");
                PrintUsage();
                return;
            }

            //Handle list if provided
            if (inputIsList)
            {
                VerbosePrint("Reading list of files from '" + inputfile + "...");
                List<string> newAppendFiles = new List<string>();
                StreamReader sr = new StreamReader(inputfile);
                inputfile = null;
                while(!sr.EndOfStream)
                {
                    string line = sr.ReadLine().Trim();
                    if (line == "") continue;
                    if (inputfile == null) inputfile = line;
                    else newAppendFiles.Add(line);
                }
                sr.Close();
                newAppendFiles.AddRange(appendFiles); //Add --append files as well
                appendFiles = newAppendFiles;
            }

            //Extract data
            EmbeddedData[] appended = new EmbeddedData[appendFiles.Count];
            if (appended.Length == 0)
            {
                VerbosePrint("Will extract data from the following file: " + inputfile);
            }
            else
            {
                VerbosePrint("Will extract data from the following files:");
                VerbosePrint(inputfile);
                for (int i = 0; i < appendFiles.Count; i++)
                {
                    VerbosePrint(appendFiles[i]);
                }
            }
            EmbeddedData data = ExtractData(inputfile, key, threshold);
            for (int i = 0; i < appended.Length; i++)
            {
                appended[i] = ExtractData(appendFiles[i], key, threshold);
            }


            //Handle data
            if (!data.hashMatches) Print("WARNING: Hash does not match data!");
            for (int i = 0; i < appended.Length; i++) { if (!appended[i].hashMatches) Print("WARNING: Hash does not match data for appended file #" + (i + 1) + "!"); }
            if (doPrint)
            {
                UTF8Encoding utf8 = new UTF8Encoding(false);
                Console.Write(utf8.GetString(data.data));
                for (int i = 0; i < appended.Length; i++) { Console.Write(utf8.GetString(appended[i].data)); }
                Console.WriteLine();
            }
            else
            {
                //Write to file
                FileStream fs = new FileStream(outputfile, FileMode.Create, FileAccess.Write);
                fs.Write(data.data, 0, data.data.Length);
                for (int i = 0; i < appended.Length; i++) { fs.Write(appended[i].data, 0, appended[i].data.Length); }
                fs.Close();
                Print("Embedded data extracted to '" + outputfile + "'.");
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: TransparentExtract.exe [options] <inputFile> [outputFile]");
            Console.WriteLine("-k [key], --key [key]: Use the specified encryption key");
            Console.WriteLine("-t [number], --threshold [number]: Maximum alpha for data pixels (0 by default)");
            Console.WriteLine("-l [listFile], --list [listFile]: Treat inputFile as a newline-separated list of input files");
            Console.WriteLine("-a [inputFile], --append [inputFile]: Also extract another file and append it to the result (can be repeated) (applied after --list)");
            Console.WriteLine("-p, --print: Display the embedded text instead of writing to a file");
            Console.WriteLine("-v, --verbose: Print verbose output");
            Console.WriteLine("-s, --silent: Do not print any output other than --print output (will still print output if invalid arguments are provided, but not on error)");
            Console.WriteLine("If no key is specified, the key '" + KEY_DEFAULT + "' will be used by default. Explicitly specify an empty "
                + "key (\"\") if you want to extract unencrypted data.");
            return;
        }

        public static EmbeddedData ExtractData(string filepath, string key, int threshold = 0)
        {
            Bitmap bmp = new Bitmap(filepath);
            return ExtractData(bmp, key, threshold);
        }

        public static EmbeddedData ExtractData(Bitmap bmp, string key, int threshold = 0)
        {
            return ParseData(ExtractRawData(bmp, threshold), key);
        }

        public static EmbeddedData ParseData(byte[] rawData, string key)
        {
            Stream inputStream = null;
            bool useEncryption = (key != null && key.Length > 0);
            if (useEncryption)
            {
                MemoryStream encryptedStream = new MemoryStream(rawData);
                //Read IV
                VerbosePrint("Reading IV...");
                byte[] iv = new byte[16];
                encryptedStream.Read(iv, 0, 16);
                //Decrypt
                VerbosePrint("Beginning decryption...");
                AesManaged aes = new AesManaged();
                Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(key, salt);
                rfc2898.IterationCount = 4854;
                inputStream = new CryptoStream(encryptedStream, aes.CreateDecryptor(rfc2898.GetBytes(32), iv), CryptoStreamMode.Read);
            }
            else
            {
                VerbosePrint("Empty key specified, not using encryption.");
                inputStream = new MemoryStream(rawData);
            }

            //Check format version
            int version = inputStream.ReadByte();
            VerbosePrint("Format version: " + version);
            if (version > VERSION_FORMAT)
            {
                Print("Data is invalid or uses newer version (" + version + ", only format version " + VERSION_FORMAT + " and below is supported)!");
                return null;
            }
            //Read other metadata
            EmbeddedData data = new EmbeddedData();
            data.contentType = (EmbeddedData.ContentType)inputStream.ReadByte();
            VerbosePrint("Content type: " + data.contentType.ToString());
            byte[] buffer = new byte[16];
            inputStream.Read(buffer, 0, 4); //Read compressed length
            int length = BitConverter.ToInt32(buffer, 0);
            VerbosePrint("Compressed data length: " + length);
            inputStream.Read(buffer, 0, 16); //Read MD5 hash
            VerbosePrint("Read hash...");
            //Read compressed data
            byte[] compressedData = new byte[length];
            inputStream.Read(compressedData, 0, length);
            VerbosePrint("Read compressed data...");
            //inputStream.Close(); //Causes 'padding is invalid' exception for CryptoStream as it attempts to FlushFinalBlock()
            //Check hash
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] hash = md5.ComputeHash(compressedData);
            data.hashMatches = true;
            VerbosePrint("Checking if hash matches...");
            for (int i = 0; i < 16; i++)
            {
                if (buffer[i] != hash[i]) { data.hashMatches = false; }
            }
            if (!data.hashMatches) Print("Warning: Hash mismatch in data!");
            else VerbosePrint("Hash matches data.");
            //Extract
            VerbosePrint("Extracting...");
            MemoryStream compressedStream = new MemoryStream(compressedData);
            DeflateStream deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress, false);
            MemoryStream dataStream = new MemoryStream();
            deflateStream.CopyTo(dataStream);
            data.data = dataStream.ToArray();
            dataStream.Close();
            deflateStream.Close();
            compressedStream.Close();
            VerbosePrint("Successfully extracted.");

            return data;
        }

        public static byte[] ExtractRawData(Bitmap bmp, int threshold = 0)
        {
            VerbosePrint("Reading data from image, threshold = " + threshold);
            MemoryStream ms = new MemoryStream(bmp.Width * bmp.Height * 3);
            for (int iy = 0; iy < bmp.Height; iy++)
            {
                for (int ix = 0; ix < bmp.Width; ix++)
                {
                    Color c = bmp.GetPixel(ix, iy);
                    if (c.A > threshold) continue;
                    ms.WriteByte(c.R);
                    ms.WriteByte(c.G);
                    ms.WriteByte(c.B);
                }
            }
            VerbosePrint("Read " + ms.Length + " bytes from image (including filler bytes).");
            return ms.ToArray();
        }

        private static void Print(string msg)
        {
            if (!silent) Console.WriteLine(msg);
        }

        private static void VerbosePrint(string msg)
        {
            if (verbose && !silent) Console.WriteLine(msg);
        }
    }
}
