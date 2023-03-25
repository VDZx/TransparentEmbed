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

namespace TransparentEmbed
{
    class ArgumentParser
    {
        string[] args = null;
        int pos = 0;
        public ArgumentParser(string[] args) { this.args = args; }
        public bool HasNext() { return pos < args.Length; }
        public string GetNext() { return ((args != null && HasNext()) ? args[pos++] : null); }
    }

    public class TransparentEmbedder
    {
        public enum ContentType
        {
            Invalid = 0x00,
            Data = 0x01,
            File = 0x02,
            Text = 0x03
        }

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
            string inputFile = null;
            string fileToEmbed = null;
            string outputFile = null;
            string key = KEY_DEFAULT;
            ContentType contentType = ContentType.File;
            int threshold = 0;
            long offset = 0;
            int bytesToRead = 0;
            while (ap.HasNext())
            {
                string arg = ap.GetNext();
                if (arg.StartsWith("-"))
                {
                    switch(arg)
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
                        case "-c":
                        case "--content":
                            if (!ap.HasNext())
                            {
                                Console.WriteLine("No argument provided for -c or --content!");
                                return;
                            }
                            string typestring = ap.GetNext();
                            switch(typestring.ToLower())
                            {
                                case "file": contentType = ContentType.File; break;
                                case "text": contentType = ContentType.Text; break;
                                case "data": contentType = ContentType.Data; break;
                                default:
                                    Console.WriteLine("Unknown content type: " + typestring);
                                    return;
                            }
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
                        case "-o":
                        case "--offset":
                            try
                            {
                                offset = Convert.ToInt64(ap.GetNext());
                                if (offset < 0) throw new Exception("Offset must be 0 or higher!");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Could not read value for offset: " + ex.ToString());
                                return;
                            }
                            break;
                        case "-l":
                        case "--length":
                            try
                            {
                                bytesToRead = Convert.ToInt32(ap.GetNext());
                                if (bytesToRead < 0) throw new Exception("Length must be 0 or higher!");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Could not read value for length: " + ex.ToString());
                                return;
                            }
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
                else if (inputFile == null) { inputFile = arg; }
                else if (fileToEmbed == null) { fileToEmbed = arg; }
                else if (outputFile == null) { outputFile = arg; }
                else
                {
                    Console.WriteLine("Too many arguments specified.");
                    PrintUsage();
                    return;
                }
            }

            if (outputFile == null) //Not enough arguments specified
            {
                Console.WriteLine("Not enough arguments specified.");
                PrintUsage();
                return;
            }

            if (key != KEY_DEFAULT) VerbosePrint("Using specified key: '" + key + "'");
            else VerbosePrint("No key specified, using default: '" + key + "'");

            if (EmbedFile(inputFile, fileToEmbed, outputFile, key, offset, bytesToRead, contentType, threshold))
            {
                Print("Successfully embedded file '" + fileToEmbed + "' into '" + inputFile + "' and exported the result as '" + outputFile + "'.");
            }
            else
            {
                Print("Failed to embed file!");
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: TransparentEmbed.exe [options] <inputFile> <fileToEmbed> <outputFile>");
            Console.WriteLine("-k [key], --key [key]: Use the specified encryption key");
            Console.WriteLine("-c [type], --content [type]: Specify content type: 'file', 'text' or 'data'");
            Console.WriteLine("-t [number], --threshold [number]: Maximum alpha for data pixels (0 by default)");
            Console.WriteLine("-o [offset], --offset [offset]: Offset in fileToEmbed to start reading at (0 by default)");
            Console.WriteLine("-l [bytes], --length [bytes]: Number of bytes in fileToEmbed to read; use 0 to read everything (0 by default)");
            Console.WriteLine("-v, --verbose: Print verbose output");
            Console.WriteLine("-s, --silent: Do not print any output (will still print output if invalid arguments are provided, but not on error)");
            Console.WriteLine("If no key is specified, the key '" + KEY_DEFAULT + "' will be used by default. Explicitly specify an empty "
                + "key (\"\") if you want to embed the data unencrypted.");
            return;
        }

        public static bool EmbedFile(string inputFile, string fileToEmbed, string outputFile, string key, long startOffset = 0, int bytesToRead = 0, ContentType contentType = ContentType.File, int threshold = 0)
        {
            VerbosePrint("Reading data to embed from file '" + fileToEmbed + "'...");
            FileStream fs = new FileStream(fileToEmbed, FileMode.Open, FileAccess.Read);
            int toRead = bytesToRead;
            if (toRead == 0 || (toRead > (fs.Length - startOffset))) { toRead = (int)(fs.Length - startOffset); }
            VerbosePrint("Reading " + toRead + " bytes from offset " + startOffset + "...");
            if (startOffset > 0) fs.Seek(startOffset, SeekOrigin.Begin);
            byte[] dataToEmbed = new byte[toRead];
            fs.Read(dataToEmbed, 0, toRead);
            fs.Close();
            VerbosePrint("Embedding data into file '" + inputFile + "', to export as '" + outputFile + "'.");
            return EmbedData(inputFile, dataToEmbed, contentType, outputFile, key, threshold, true);
        }

        public static bool EmbedData(string inputFile, byte[] data, ContentType contentType, string outputFile, string key, int threshold = 0, bool fillWithRandom = true)
        {
            VerbosePrint("Loading input file...");
            Bitmap bmp = new Bitmap(inputFile);
            bmp = ConvertBitmap(bmp);
            bmp = EmbedData(bmp, data, contentType, key, threshold, fillWithRandom);
            if (bmp == null) return false;
            VerbosePrint("Exporting file...");
            bmp.Save(outputFile);
            VerbosePrint("Successfully exported file!");
            return true;
        }

        public static Bitmap EmbedData(Bitmap bmp, byte[] data, ContentType contentType, string key, int threshold = 0, bool fillWithRandom = true)
        {
            bool useEncryption = (key != null && key.Length > 0);

            //Compress
            VerbosePrint("Compressing...");
            MemoryStream msCompressed = new MemoryStream();
            DeflateStream ds = new DeflateStream(msCompressed, CompressionLevel.Optimal, true);
            ds.Write(data, 0, data.Length);
            ds.Close();
            int cSize = (int)msCompressed.Length;

            //Prefix data with IV, version, content type, length and MD5 hash
            MemoryStream msEmbedded = new MemoryStream(cSize + (useEncryption ? 16 : 0) + 1 + 1 + 4 + 16);
            AesManaged aes = null;
            if (useEncryption)
            {
                aes = new AesManaged();
                aes.GenerateIV();
                VerbosePrint("IV generated.");
                msEmbedded.Write(aes.IV, 0, 16);
            }
            VerbosePrint("Format version: " + VERSION_FORMAT);
            msEmbedded.WriteByte(VERSION_FORMAT);
            VerbosePrint("Content type: " + contentType + " (" + (byte)contentType + ")");
            msEmbedded.WriteByte((byte)contentType);
            VerbosePrint("Compressed data size: " + cSize);
            msEmbedded.Write(BitConverter.GetBytes(cSize), 0, 4);
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] hash = md5.ComputeHash(msCompressed.ToArray(), 0, cSize); //Have to use array; stream gives wrong hash
            VerbosePrint("Hash generated.");
            msEmbedded.Write(hash, 0, 16);
            msCompressed.WriteTo(msEmbedded);
            VerbosePrint("Total size to embed: " + msEmbedded.Length);

            //Encrypt
            byte[] finalData = null;
            if (useEncryption)
            {
                VerbosePrint("Encrypting...");
                MemoryStream msEncrypted = null;
                msEncrypted = new MemoryStream((int)msEmbedded.Length);
                Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(key, salt);
                rfc2898.IterationCount = 4854;
                CryptoStream cs = new CryptoStream(msEncrypted, aes.CreateEncryptor(rfc2898.GetBytes(32), aes.IV), CryptoStreamMode.Write);
                msEmbedded.WriteTo(cs);
                cs.FlushFinalBlock();
                cs.Close();
                finalData = msEncrypted.ToArray();
            }
            else
            {
                VerbosePrint("Empty key specified, not applying encryption.");
                finalData = msEmbedded.ToArray();
            }
            msCompressed.Close();

            //Embed
            return EmbedRawData(bmp, finalData, threshold, fillWithRandom);
        }

        public static Bitmap EmbedRawData(Bitmap bmp, byte[] data, int threshold = 0, bool fillWithRandom = true)
        {
            VerbosePrint("Embedding data into bitmap...");
            Random random = (fillWithRandom ? new Random() : null);
            long pos = 0; //Position in data buffer

            byte[] randomBytes = (fillWithRandom ? new byte[bmp.Width * 3] : null);
            for (int iy = 0; iy < bmp.Height; iy++)
            {
                if (fillWithRandom && pos + bmp.Width >= data.Length) random.NextBytes(randomBytes); //Use random data once done writing
                for (int ix = 0; ix < bmp.Width; ix++)
                {
                    Color c = bmp.GetPixel(ix, iy);
                    if (c.A > threshold) continue; //Skip pixels that aren't transparent (enough)
                    byte b1 = ((pos < data.Length) ? data[pos++] : (fillWithRandom ? randomBytes[ix * 3 + 0] : c.R));
                    byte b2 = ((pos < data.Length) ? data[pos++] : (fillWithRandom ? randomBytes[ix * 3 + 0] : c.G));
                    byte b3 = ((pos < data.Length) ? data[pos++] : (fillWithRandom ? randomBytes[ix * 3 + 0] : c.B));
                    bmp.SetPixel(ix, iy, Color.FromArgb(c.A, b1, b2, b3));
                }
            }
            if (pos < data.Length)
            {
                Print("Failed to embed data! Not enough transparent pixels available to embed all data!");
                return null;
            }
            VerbosePrint("Embedded data into bitmap.");
            return bmp;
        }

        private static Bitmap ConvertBitmap(Bitmap input)
        {
            if (input.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb) return input;
            Bitmap toReturn = new Bitmap(input.Width, input.Height);
            for (int iy = 0; iy < input.Height; iy++)
            {
                for (int ix = 0; ix < input.Width; ix++)
                {
                    toReturn.SetPixel(ix, iy, input.GetPixel(ix, iy));
                }
            }
            return toReturn;
        }

        private static void Print(string msg)
        {
            if (!silent) Console.WriteLine(msg);
        }

        private static void VerbosePrint(string msg)
        {
            if (verbose) Console.WriteLine(msg);
        }



        //=== Stuff below is not used internally, but could be useful for other programs ===

        public static void EmbedRawData(string inputFile, byte[] data, string outputFile, int threshold = 0, bool fillWithRandom = true)
        {
            VerbosePrint("Loading input file...");
            Bitmap bmp = new Bitmap(inputFile);
            bmp = EmbedRawData(bmp, data, threshold, fillWithRandom);
            VerbosePrint("Exporting file...");
            bmp.Save(outputFile);
            VerbosePrint("Successfully exported file!");
        }
    }
}
