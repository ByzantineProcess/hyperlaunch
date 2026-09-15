
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Hyperlaunch.Download;

namespace Hyperlaunch.Utilities.JarTools;

public static class Patching
{
    // i will lose my life to binary formats
    // ref: https://www.w3.org/TR/NOTE-gdiff-19970901
    
    public static void PatchJarWithNeoForgeBundle(string jarPath, byte[] lzmaCompressedNfBundleBytes, string neoForgeVersion)
    {
        string patchedJarPath = $"{DownloadTask.BaseLibraryPath}/net/neoforged/minecraft-client-patched/{neoForgeVersion}/minecraft-client-patched-{neoForgeVersion}.jar";
        Directory.CreateDirectory($"{DownloadTask.BaseLibraryPath}/net/neoforged/minecraft-client-patched/{neoForgeVersion}/");
        SevenZip.Compression.LZMA.Decoder decompressor = new SevenZip.Compression.LZMA.Decoder();
        using MemoryStream lzmaCompressedNfBundle = new MemoryStream(lzmaCompressedNfBundleBytes);

        byte[] properties = new byte[5];
        lzmaCompressedNfBundle.ReadExactly(properties);
        byte[] sizeBytes = new byte[8]; 
        lzmaCompressedNfBundle.ReadExactly(sizeBytes);
        long size = BinaryPrimitives.ReadInt64LittleEndian(sizeBytes);

        decompressor.SetDecoderProperties(properties);

        using MemoryStream memoryStream = new MemoryStream();

        // this *might* block for a while
        decompressor.Code(lzmaCompressedNfBundle, memoryStream, -1, size, null);

        byte[] magic = new byte[16];
        memoryStream.Seek(0, SeekOrigin.Begin);
        memoryStream.ReadExactly(magic);

        if (Encoding.UTF8.GetString(magic) != "NFPATCHBUNDLE001")
        {
            throw new Exception("Could not recognise NeoForge magic bytes");
        }

        // most code from here on out is based on an AI's documentation of the NFPATCHBUNDLE001 spec
        // i wrote all the code, it just told me which bytes to read where
        // there's no other online documentation for this that I could find

        byte[] eCountBytes = new byte[4];
        memoryStream.ReadExactly(eCountBytes);
        int eCount = BinaryPrimitives.ReadInt32BigEndian(eCountBytes);
        Log.Print($"there are {eCount} entries in the patch bundle");

        byte[] distributionBits = new byte[1];
        memoryStream.ReadExactly(distributionBits);
        // bitwise math is scary and this shouldn't actually mean anything so i'll ignore the distribution byte for now

        // now the patching can start
        File.Copy(jarPath, patchedJarPath, true);

        ZipArchive clientJarForReading = ZipFile.Open(patchedJarPath, ZipArchiveMode.Read);

        List<string> jarEntries = clientJarForReading.Entries.Select(entry => entry.FullName).ToList();

        clientJarForReading.Dispose();

        ZipArchive clientJar = ZipFile.Open(patchedJarPath, ZipArchiveMode.Update);

        foreach (string entry in jarEntries)
        {
            if (entry.StartsWith("META-INF/") && 
                (entry.EndsWith(".SF") ||
                entry.EndsWith(".RSA") ||
                entry.EndsWith(".DSA") ||
                entry.EndsWith(".EC")) &&
                entry.Split('/').Length == 2)
            {
                clientJar.GetEntry(entry).Delete();
            }
        }

        for (int i = 0; i < eCount; i++)
        {
            // i did Not have to google how to do bit masks no no
            byte[] flagBits = new byte[1];
            memoryStream.ReadExactly(flagBits);
            byte operationBits = (byte)(flagBits[0] & 0x18);
            OpType activeOperation = operationBits switch
            {
                0x00 => OpType.CREATE,
                0x08 => OpType.MODIFY,
                0x10 => OpType.REMOVE,
                _ => throw new Exception($"Unrecognised op bit in NeoForge bundle {operationBits}"),
            };
            byte[] pathLengthBytes = new byte[2];
            memoryStream.ReadExactly(pathLengthBytes);
            ushort pathLength = BinaryPrimitives.ReadUInt16BigEndian(pathLengthBytes);

            // allegedly the reference reader throws here so I will too
            if (pathLength > 65535)
            {
                throw new Exception("Path length out of range in NeoForge bundle");
            }

            // Log.Print("gaming");

            byte[] pathBytes = new byte[pathLength];
            memoryStream.ReadExactly(pathBytes);
            string path = Encoding.ASCII.GetString(pathBytes);

            // seek past other data for testing
            if (activeOperation == OpType.MODIFY)
            {
                memoryStream.Seek(4, SeekOrigin.Current);
                // skip for now?
            }
            
            byte[] dataLenBytes = new byte[4];
            memoryStream.ReadExactly(dataLenBytes);
            int dataLen = BinaryPrimitives.ReadInt32BigEndian(dataLenBytes);

            byte[] dataBytes = new byte[dataLen];
            memoryStream.ReadExactly(dataBytes);

            switch (activeOperation)
            {
                case OpType.CREATE:
                    // Log.Print($"doing CREATE call on {path}");
                    clientJar.CreateEntry(path);
                    Stream newFile = clientJar.GetEntry(path).Open();
                    newFile.Write(dataBytes);
                    newFile.Close();
                    break;

                case OpType.MODIFY:
                    // oh hell naw
                    // Log.Print($"doing MODIFY call on {path}");
                    byte[] fileBytes = new byte[clientJar.GetEntry(path).Length];
                    Stream fs = clientJar.GetEntry(path).Open();
                    fs.ReadExactly(fileBytes);
                    fs.Close();
                    clientJar.GetEntry(path).Delete();
                    Stream modifiedFile = clientJar.CreateEntry(path).Open();
                    modifiedFile.Write(ApplyGdiff(fileBytes, dataBytes, activeOperation));
                    modifiedFile.Flush();
                    break;

                case OpType.REMOVE:
                    // Log.Print($"doing REMOVE call on {path}");
                    clientJar.GetEntry(path).Delete();
                    break;

                default:
                    throw new Exception("something has gone horribly wrong patching NeoForge");
            }
        }
        clientJar.Dispose();
    }

    public static byte[] ApplyGdiff(byte[] file, byte[] payload, OpType opType)
    {
        byte[] expected = [0xD1, 0xFF, 0xD1, 0xFF, 0x04];
        if (!payload[0..5].SequenceEqual(expected))
        {
            throw new Exception($"Unrecognised gdiff magic in NeoForge bundle. Got {PrettyToString.ByteArray(file[0..5])} instead of {PrettyToString.ByteArray(expected)}. Active opcode was {StringEnum.Retrieve(opType)}");
        }
        int position = 5;
        using MemoryStream output = new MemoryStream();
        bool done = false;
        // Log.Print($"gdiff payload length is {payload.Length}");
        while (true)
        {
            int opcode = payload[position];
            position++;
            switch (opcode)
            {
                case 0:
                    done = true;
                    break;

                case >= 1 and <= 246:
                    output.Write(payload, position, opcode);
                    position += opcode;
                    break;
                
                case 247:
                    ushort len247 = BinaryPrimitives.ReadUInt16BigEndian(payload.AsSpan(position));
                    position += 2;
                    output.Write(payload, position, len247);
                    position += len247;
                    break;
                
                case 248:
                    int len248 = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(position));
                    position += 4;
                    output.Write(payload, position, len248);
                    position += len248;
                    break;
                
                case >= 249 and <= 255:

                    long offset = 0;
                    int length = 0;

                    if (opcode == 249)
                    {
                        offset = BinaryPrimitives.ReadUInt16BigEndian(payload.AsSpan(position));
                        position += 2;
                        length = payload[position];
                        position++;
                    }
                    if (opcode == 250)
                    {
                        offset = BinaryPrimitives.ReadUInt16BigEndian(payload.AsSpan(position));
                        position += 2;
                        length = BinaryPrimitives.ReadUInt16BigEndian(payload.AsSpan(position));
                        position += 2;
                    }
                    if (opcode == 251)
                    {
                        offset = BinaryPrimitives.ReadUInt16BigEndian(payload.AsSpan(position));
                        position += 2;
                        length = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(position));
                        position += 4;
                    }
                    if (opcode == 252)
                    {
                        offset = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(position));
                        position += 4;
                        length = payload[position];
                        position++;
                    }
                    if (opcode == 253)
                    {
                        offset = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(position));
                        position += 4;
                        length = BinaryPrimitives.ReadUInt16BigEndian(payload.AsSpan(position));
                        position += 2;
                    }
                    if (opcode == 254)
                    {
                        offset = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(position));
                        position += 4;
                        length = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(position));
                        position += 4;
                    }
                    if (opcode == 255)
                    {
                        offset = BinaryPrimitives.ReadInt64BigEndian(payload.AsSpan(position));
                        position += 8;
                        length = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(position));
                        position += 4;
                    }

                    if (offset < 0 || offset + length > file.Length) { throw new Exception("wee woo wee woo"); }

                    output.Write(file, (int)offset, length);
                    break;

                default:
                    throw new Exception("Unrecognised gdiff opcode");
            }
            if (done)
            {
                break;
            }
            // Log.Print($"finished an opcode {opcode}");
        }
        byte[] byteOut = new byte[output.Length];
        output.Seek(0, SeekOrigin.Begin);
        output.ReadExactly(byteOut);
        return byteOut;
    }
}

public enum OpType
{
    [StringEnum("CREATE")]
    CREATE,
    [StringEnum("MODIFY")]
    MODIFY,
    [StringEnum("REMOVE")]
    REMOVE
}