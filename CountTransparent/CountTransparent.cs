/*
TransparentEmbed - Tools to embed and extract data into and from transparent pixels
Written in 2023 by VDZ
To the extent possible under law, the author(s) have dedicated all copyright and related and neighboring rights to this software to the public domain worldwide. This software is distributed without any warranty.
You should have received a copy of the CC0 Public Domain Dedication along with this software. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.
*/
//TL;DR for above notice: You can do whatever you want with this including commercial use without any restrictions or requirements.

using System;
using System.Drawing;
using System.Collections.Generic;

namespace CountTransparent
{
    class ArgumentParser
    {
        string[] args = null;
        int pos = 0;
        public ArgumentParser(string[] args) { this.args = args; }
        public bool HasNext() { return pos < args.Length; }
        public string GetNext() { return ((args != null && HasNext()) ? args[pos++] : null); }
    }

    class CountTransparent
    {
        public static void Main(string[] args)
        {
            ArgumentParser ap = new ArgumentParser(args);
            List<string> inputFiles = new List<string>();
            int threshold = 0;
            while(ap.HasNext())
            {
                string arg = ap.GetNext();
                if (arg.StartsWith("-"))
                {
                    switch(arg)
                    {
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
                        default:
                            Console.WriteLine("Unrecognized option: " + arg);
                            PrintUsage();
                            return;
                    }
                }
                else { inputFiles.Add(arg); }
            }

            if (inputFiles.Count == 0)
            {
                Console.WriteLine("No input files specified!");
                PrintUsage();
                return;
            }

            int totalCount = 0;
            for (int i = 0; i < inputFiles.Count; i++)
            {
                int count = CountTransparentPixels(inputFiles[i], threshold);
                totalCount += count;
                if (inputFiles.Count > 1)
                {
                    Console.WriteLine("File #" + (i + 1) + " has " + count + " transparent pixels -> " + (count * 3) + " bytes");
                }
            }
            int bytes = totalCount * 3;
            int kilobytes = bytes / 1024;
            int megabytes = kilobytes / 1024;
            Console.WriteLine("Total: " + totalCount + " transparent pixels -> " + bytes + " bytes (" + kilobytes + " KB, " + megabytes + " MB)");
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: CountTransparent [options] <inputFile> [inputFile2] [inputFile3] [inputFile...]");
            Console.WriteLine("-t [number], --threshold [number]: Maximum alpha for data pixels (0 by default)");
            return;
        }

        public static int CountTransparentPixels(string filepath, int threshold = 0)
        {
            return CountTransparentPixels(new Bitmap(filepath), threshold);
        }

        public static int CountTransparentPixels(Bitmap bmp, int threshold = 0)
        {
            int count = 0;
            for (int iy = 0; iy < bmp.Height; iy++)
            {
                for (int ix = 0; ix < bmp.Width; ix++)
                {
                    if (bmp.GetPixel(ix, iy).A <= threshold) count++;
                }
            }
            return count;
        }
    }
}
