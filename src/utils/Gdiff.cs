
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Hyperlaunch.Download;

namespace Hyperlaunch.Utilities;

public static class Gdiff
{
    // i will lose my life to binary formats
    // ref: https://www.w3.org/TR/NOTE-gdiff-19970901
    
    public static void PatchJarWithNeoForgeBundle(string jarPath, byte[] lzmaCompressedNfBundleBytes, string gameVersion, string neoFormVersion)
    {
        string patchedJarPath = $"{DownloadTask.BaseLibraryPath}/net/minecraft/client/{gameVersion}-{neoFormVersion}/client-{gameVersion}-{neoFormVersion}-srg.jar";
        SevenZip.Compression.LZMA.Decoder decompressor = new SevenZip.Compression.LZMA.Decoder();
        using MemoryStream lzmaCompressedNfBundle = new MemoryStream(lzmaCompressedNfBundleBytes);

        byte[] hexdump = new byte[16];

        lzmaCompressedNfBundle.ReadExactly(hexdump);
        lzmaCompressedNfBundle.Seek(0, SeekOrigin.Begin);

        Log.Print($"lzmaNfBundle Hexdump: {BitConverter.ToString(hexdump)}, input bytes hash: {Sha1.Compute(lzmaCompressedNfBundleBytes)}");

        byte[] hashCheck = new byte[lzmaCompressedNfBundle.Length];
        lzmaCompressedNfBundle.ReadExactly(hashCheck);
        Log.Print($"MemoryStream hash is {Sha1.Compute(hashCheck)}");

        lzmaCompressedNfBundle.Seek(0, SeekOrigin.Begin);

        byte[] properties = new byte[5];
        lzmaCompressedNfBundle.ReadExactly(properties);
        byte[] sizeBytes = new byte[8]; 
        lzmaCompressedNfBundle.ReadExactly(sizeBytes);
        long size = BinaryPrimitives.ReadInt64LittleEndian(sizeBytes);

        decompressor.SetDecoderProperties(properties);
        // decompressor.SetDecoderProperties([0x5D, 0x00, 0x00, 0x00, 0x01]);

        // lzmaCompressedNfBundle.Seek(0, SeekOrigin.Current);

        Log.Print($"nf bundle size is {size} bytes, bundle stream position is {lzmaCompressedNfBundle.Position}, next byte is {lzmaCompressedNfBundle.ReadByte()}");
        lzmaCompressedNfBundle.Seek(-1, SeekOrigin.Current);

        // byte[] buf = new byte[size];

        using MemoryStream memoryStream = new MemoryStream();


        // this *might* block for a while
        decompressor.Code(lzmaCompressedNfBundle, memoryStream, lzmaCompressedNfBundle.Length - 14, size, null);

        byte[] magic = new byte[16];
        memoryStream.Seek(0, SeekOrigin.Begin);
        memoryStream.ReadExactly(magic);

        Log.Print("this should probably say NFPATCHBUNDLE001 -> " + Encoding.UTF8.GetString(magic));
    }
}