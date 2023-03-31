TransparentEmbed allows you to invisibly hide (encrypted) data in the RGB values of transparent pixels of images with transparency. You can then use TransparentExtract to extract this data again. CountTransparent lets you check how much data can be stored in an image.

# Usage

## General

The .exe files can be used as .NET libraries if you want to integrate them into another program. (You can rename them to .dll or just add them directly as .exe references.) All relevant functions should be exported.

## TransparentEmbed

TransparentEmbed.exe [options] \<inputFile\> \<fileToEmbed\> \<outputFile\>

The following options are supported:

* -k [key], --key [key]: Use the specified encryption key
* -c [type], --content [type]: Specify content type: 'file', 'text' or 'data'
* -t [number], --threshold [number]: Maximum alpha for data pixels (0 by default)
* -o [offset], --offset [offset]: Offset in fileToEmbed to start reading at (0 by default)
* -l [bytes], --length [bytes]: Number of bytes in fileToEmbed to read; use 0 to read everything (0 by default)
* -b [num], --begin [num]: Skip the first num eligible (i.e. sufficiently transparent) bytes (they will not be randomized either) (0 by default)
* -e [num], --end [num]: Do not write beyond num eligible (i.e. sufficiently transparent) bytes (no randomization beyond this point either); use 0 to use everything past --start (0 by default; value is exclusive)
* -v, --verbose: Print verbose output
* -s, --silent: Do not print any output (will still print output if invalid arguments are provided, but not on error)

If no key is specified, the key 'SecureBeneathTheWatchfulEyes' will be used by default. Explicitly specify an empty key ("") if you want to embed the data unencrypted.

It should go without saying that if you don't want anyone stumbling upon the image to be able to extract the contents, you should specify a custom key rather than using the default.

## TransparentExtract

Usage: TransparentExtract.exe [options] \<inputFile\> [outputFile]

The following options are supported:

* -k [key], --key [key]: Use the specified encryption key
* -t [number], --threshold [number]: Maximum alpha for data pixels (0 by default)
* -b [num], --begin [num]: Only start reading after num eligible (i.e. sufficiently transparent) bytes (0 by default)
* -l [listFile], --list [listFile]: Treat inputFile as a newline-separated list of input files
* -a [inputFile], --append [inputFile]: Also extract another file and append it to the result (can be repeated) (applied after --list)
* -p, --print: Display the embedded text instead of writing to a file
* -v, --verbose: Print verbose output
* -s, --silent: Do not print any output other than --print output (will still print output if invalid arguments are provided, but not on error)

If no key is specified, the key 'SecureBeneathTheWatchfulEyes' will be used by default. Explicitly specify an empty key ("") if you want to extract unencrypted data.

## CountTransparent

Usage: CountTransparent.exe [options] \<inputFile\>

Only the threshold option is supported:

* -t [number], --threshold [number]: Maximum alpha for data pixels (0 by default)

# How it works and format spec

Each pixel consists of four byte values: red, green, blue and alpha. The latter determines the degree of transparency, with 255 being not at all transparent and 0 being fully transparent. Even fully transparent pixels have RGB values, so we use them to store data without affecting the image visually. (Using the threshold option you can also embed data into slightly transparent pixels, but this will be very subtly visible in the image; by default it only uses fully transparent pixels.) They are encoded left to right, top to bottom starting at the top left, skipping all non-transparent (or insufficiently transparent if threshold > 0) pixels.

Unless an empty key is explicitly specified, the data will be encrypted using AES256. The first 16 bytes of the data will be the IV. After that follows encrypted data. It starts with the header:

* (1 byte) Format version, currently 0x01
* (1 byte) Content type:
* * 0x00 - Invalid
* * 0x01 - Data
* * 0x02 - File
* * 0x03 - Text
* (4 bytes) Length of (compressed) data
* (16 bytes) MD5 hash of (compressed) data

Beyond that is the data, with compressed length as specified in the header, compressed using the DEFLATE algorithm. It may be (and is by default) followed by unencrypted random data. If an empty key is used, the IV is skipped and the embeded data starts immediately with the format version. It is otherwise identical, save for being unencrypted.

## Encryption details

The encryption used is AES256 with a randomly generated IV (16 bytes), which is prepended to the encrypted data. The key used for the encryption is derived from the user-provided key (or 'SecureBeneathTheWatchfulEyes' if none is provided), hashed using the PBKDF2 algorithm with 4854 cycles using the salt 41726775696e67207468617420796f7520646f6e277420636172652061626f75742074686520726967687420746f2070726976616379206265636175736520796f752068617665206e6f7468696e6720746f2068696465206973206e6f20646966666572656e74207468616e20736179696e6720796f7520646f6e277420636172652061626f7574206672656520737065656368206265636175736520796f752068617665206e6f7468696e6720746f207361792e ('Arguing that you don't care about the right to privacy because you have nothing to hide is no different than saying you don't care about free speech because you have nothing to say.'). Note that data beyond the indicated length may (and likely will) be random and whatever function you use for decryption might break if you read beyond the encrypted data.

# Examples

## Simple embed

The following image contains the source and binaries of the TransparentEmbed tools as well as the RelativeEmbed tools, using the default key:

![embeddingtools](https://user-images.githubusercontent.com/1906108/227747064-f1a79416-d824-400d-8dbe-6fb2cd9491bf.png)
<sub>(Original image credit: Nekotoufu, Onimai LINE stickers)</sub>

* It was embedded by running: TransparentEmbed.exe "laughing mahiro.png" embeddingtools.zip output.png
* It can be extracted by running: TransparentExtract.exe input.png output.zip

## Split embed

If you have more data than you can fit in an image, you can split it over multiple images:

![a_embed](https://user-images.githubusercontent.com/1906108/227747181-8b87db2a-11bd-4955-8ba0-25a9d0289e84.png)
<sub>(Original image credit: はる🌸, https://www.pixiv.net/member_illust.php?mode=medium&illust_id=104881993)</sub>
![b_embed](https://user-images.githubusercontent.com/1906108/227747186-65fd4009-4b3f-4a0e-99c7-94caf98dbef1.png)
<sub>(Original image credit: ゆゆり, https://www.pixiv.net/member_illust.php?mode=medium&illust_id=105889764)</sub>

* Creation of part 1: TransparentEmbed.exe -l 3230000 a_original.png Impermanence.mp3 a_embedded.png
* Creation of part 2: TransparentEmbed.exe -o 3230000 b_original.png Impermanence.mp3 b_embedded.png
* Extract it by running: TransparentExtract.exe --append b.png a.png output.mp3

This outputs a 4.8 MB MP3 file from two normal-looking image files of 4.2 MB and 3.7 MB.

## Multiple embeds in one image

Starting with version 2, you can embed in specific ranges, allowing for multiple embeds in one image (with the same key, or with different keys). The following commands were used to create this image with 8 different embeds all with different keys:

* TransparentEmbed.exe --content text --end 30000 "v.png" "v-index.txt" "v1.png"
* TransparentEmbed.exe --key "RepoURL" --content text --begin 30000 --end 60000 "v1.png" "repourl.txt" "v2.png"
* TransparentEmbed.exe --key "TransparentEmbedSource" --content text --begin 60000 --end 90000 "v2.png" "TransparentEmbedder.cs" "v3.png"
* TransparentEmbed.exe --key "TransparentEmbedBinary" --content file --begin 90000 --end 120000 "v3.png" "TransparentEmbed.exe" "v4.png"
* TransparentEmbed.exe --key "TransparentExtractSource" --content text --begin 120000 --end 150000 "v4.png" "TransparentExtractor.cs" "v5.png"
* TransparentEmbed.exe --key "TransparentExtractBinary" --content file --begin 150000 --end 180000 "v5.png" "TransparentExtract.exe" "v6.png"
* TransparentEmbed.exe --key "CountTransparentSource" --content text --begin 180000 --end 210000 "v6.png" "CountTransparent.cs" "v7.png"
* TransparentEmbed.exe --key "CountTransparentBinary" --content file --begin 210000 --end 240000 "v7.png" "CountTransparent.exe" "v8.png"

![v8](https://user-images.githubusercontent.com/1906108/229020215-f74dfc23-8648-4676-aa43-ada27ee09b3d.png)
<sub>(Couldn't identify artist)</sub>

Extracting from position 0 (default) with the default key gives you a list with positions and keys, plus a bash script to extract it all:
```
$ mono TransparentExtract.exe --print v8.png
0 - SecureBeneathTheWatchfulEyes (gives this list)
30000 - RepoURL
60000 - TransparentEmbedSource
90000 - TransparentEmbedBinary
120000 - TransparentExtractSource
150000 - TransparentExtractBinary
180000 - CountTransparentSource
210000 - CountTransparentBinary

You can use the following bash script to extract everything:
#!/bin/sh
mkdir out
mono TransparentExtract.exe "v1.png" "out/v-index.txt"
mono TransparentExtract.exe --key "RepoURL" --begin 30000 "v8.png" "out/repourl.txt"
mono TransparentExtract.exe --key "TransparentEmbedSource" --begin 60000 "v8.png" "out/TransparentEmbedder.cs"
mono TransparentExtract.exe --key "TransparentEmbedBinary" --begin 90000 "v8.png" "out/TransparentEmbed.exe"
mono TransparentExtract.exe --key "TransparentExtractSource" --begin 120000 "v8.png" "out/TransparentExtractor.cs"
mono TransparentExtract.exe --key "TransparentExtractBinary" --begin 150000 "v8.png" "out/TransparentExtract.exe"
mono TransparentExtract.exe --key "CountTransparentSource" --begin 180000 "v8.png" "out/CountTransparent.cs"
mono TransparentExtract.exe --key "CountTransparentBinary" --begin 210000 "v8.png" "out/CountTransparent.exe"
```

(Note that in this example, no random padding is used beyond the last file due to file size constraints on GitHub and elsewhere. Ideally you omit the --end for the last file and leave the embedded data indistinguishable from random noise.)
