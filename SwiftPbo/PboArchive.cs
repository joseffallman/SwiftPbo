/*
 *  This file is part of SwiftPbo.
 *  
 *  Copyright 2015-2016, 2018 by headswe
 *  Copyright 2018 by dedmen
 *  Copyright 2019 by Josef Fällman
 *  
 *  Licensed under GNU Lesser General Public License 3.0
 *  
 *  
 *  File created:    2019-06-16
 *  Github location: https://github.com/joseffallman/SwiftPbo
 *
 *  SwiftPbo is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  SwiftPbo is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with SwiftPbo.  If not, see<https://www.gnu.org/licenses/>.
 *  
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SwiftPbo
{
    public enum PackingType
    {
        Uncompressed,
        Packed,
        Encrypted
    };

    public class PboArchive : IDisposable
    {
        private ProductEntry _productEntry = new ProductEntry("", "", "", new List<Additional>());
        private List<FileEntry> _files = new List<FileEntry>();
        private string _path;
        private long _dataStart;
        private FileStream _stream;
        private byte[] _checksum;
        
        private static readonly List<char> InvaildFile = Path.GetInvalidFileNameChars().ToList();

        public PboArchive() {}

        public static bool Create(string directoryPath, string outpath = null, Config.FilterFileConfig config = null)
        {
            // Prefix files
            var pboHeader = new ProductEntry("prefix","","",new List<Additional>());
            var files = Directory.GetFiles(directoryPath, "$*$");
            foreach (var file in files)
            {
                var varname = System.IO.Path.GetFileNameWithoutExtension(file).Trim('$');
                var data = File.ReadAllText(file).Split('\n')[0];
                switch (varname.ToLowerInvariant())
                {
                    case "pboprefix":
                        pboHeader.Prefix = data;
                        break;
                    case "prefix":
                        pboHeader.Prefix = data;
                        break;
                    case "version":
                        pboHeader.ProductVersion = data;
                        break;
                    default:
                        pboHeader.Addtional.Add(new Additional(varname, data));
                        break;
                }
            }
            return Create(directoryPath, outpath, pboHeader, config);
        }
        public static bool Create(string directoryPath, string outpath, ProductEntry productEntry, Config.FilterFileConfig config = null)
        {
            var dir = new DirectoryInfo(directoryPath);
            if (!dir.Exists)
                throw new DirectoryNotFoundException();
            directoryPath = dir.FullName;

            // Check outpath.
            if (string.IsNullOrEmpty(outpath))
            {
                outpath = Directory.GetParent(directoryPath).FullName;
            }
            // Create dir if it does not exist.
            else if (!Directory.Exists(System.IO.Path.GetDirectoryName(outpath)))
            {
                try
                {
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outpath));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return false;
                }

            }

            // Check file extension, otherwise add extension.
            if (!outpath.ToLower().EndsWith(".pbo")) { 
                string pboName = Directory.GetParent(directoryPath).Name + ".pbo";
                outpath = System.IO.Path.Combine(outpath, pboName);
            }

            // Check if output file exist and rename it.
            if (File.Exists(outpath))
            {
                string backup = $"{outpath}.bak";
                if (File.Exists(backup))
                    File.Delete(backup);
                File.Move(outpath, backup);
            }
            

            // Grab all files.
            var files = new PboArchive().GetFiles(directoryPath, config );
            var entries = new List<FileEntry>();
            foreach (string file in files)
            {
                if(System.IO.Path.GetFileName(file).StartsWith("$") && System.IO.Path.GetFileName(file).EndsWith("$"))
                    continue;
                FileInfo info = new FileInfo(file);
                string path = PboUtilities.GetRelativePath(info.FullName, directoryPath);
                entries.Add(new FileEntry(path, 0x0, (ulong) info.Length, (ulong) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds, (ulong) info.Length));
            }

            // Start writing pbo.
            try
            {
                using (var stream = File.Create(outpath))
                {
                    stream.WriteByte(0x0);
                    WriteProductEntry(productEntry, stream);
                    stream.WriteByte(0x0);
                    entries.Add(new FileEntry(null, "", 0, 0, 0, 0, _file));
                    foreach (var entry in entries)
                    {
                        WriteFileEntry(stream, entry);
                    }
                    entries.Remove(entries.Last());
                    foreach (var entry in entries)
                    {
                        var buffer = new byte[2949120];
                        using (var open = File.OpenRead(System.IO.Path.Combine(directoryPath, entry.FileName)))
                        {
                            var read = 4324324;
                            while (read > 0)
                            {
                                read = open.Read(buffer, 0, buffer.Length);
                                stream.Write(buffer, 0, read);
                            }
                        }
                    }
                    stream.Position = 0;
                    byte[] hash;
                    using (var sha1 = new SHA1Managed())
                    {
                        hash = sha1.ComputeHash(stream);
                    }
                    stream.WriteByte(0x0);
                    stream.Write(hash, 0, 20);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            return true;
        }

        private static readonly List<char> InvaildPath = Path.GetInvalidPathChars().ToList();
        private static byte[] _file;

        public static string SterilizePath(string path)
        {
            
            var arr = System.IO.Path.GetDirectoryName(path).ToCharArray();
            var builder = new StringBuilder(arr.Count());
            string dirpath = System.IO.Path.GetDirectoryName(path);
            for (int i = 0; i < dirpath.Length; i++)
            {
                if (!InvaildPath.Contains(path[i]) && path[i] != System.IO.Path.AltDirectorySeparatorChar)
                    builder.Append(path[i]);
                if(path[i] == System.IO.Path.AltDirectorySeparatorChar)
                    builder.Append(System.IO.Path.DirectorySeparatorChar);
            }
            var filename = System.IO.Path.GetFileName(path).ToCharArray();
            for (int i = 0; i < filename.Length; i++)
            {
                var ch = filename[i];
                if (!InvaildFile.Contains(ch) && ch != '*' && !IsLiteral(ch))
                {
                    continue;
                }
                filename[i] = ((char)(Math.Min(90, 65 + ch % 5)));
            }
            return System.IO.Path.Combine(builder.ToString(), new string(filename));
        }

        private static List<char> _literalList = new List<char>() {'\'','\"','\\','\0','\a','\b','\f','\n','\r','\t','\v'};
        private static bool IsLiteral(char ch)
        {
            return _literalList.Contains(ch);
        }

        public static void Clone(string path, ProductEntry productEntry, Dictionary<FileEntry, string> files, byte[] checksum = null)
        {
            try
            {
                if (!Directory.Exists(System.IO.Path.GetDirectoryName(path)) && !string.IsNullOrEmpty(System.IO.Path.GetDirectoryName(path)))
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path) );
                using (var stream = File.Create(path))
                {
                    stream.WriteByte(0x0);
                    WriteProductEntry(productEntry, stream);
                    stream.WriteByte(0x0);
                    files.Add(new FileEntry(null, "", 0, 0, 0, 0, _file), "");
                    foreach (var entry in files.Keys)
                    {
                        WriteFileEntry(stream, entry);
                    }
                    files.Remove(files.Last().Key);
                    foreach (var file in files.Values)
                    {
                        var buffer = new byte[2949120];
                        using (var open = File.OpenRead(file))
                        {
                            int bytesRead;
                            while ((bytesRead =
                                         open.Read(buffer, 0, 2949120)) > 0)
                            {
                                stream.Write(buffer, 0, bytesRead);
                            }
                        }
                    }
                    if(checksum != null && checksum.Any(b => b!=0))
                    {
                        stream.WriteByte(0x0);
                        stream.Write(checksum, 0, checksum.Length);
                    }
                    else if (checksum == null)
                    {
                        stream.Position = 0;
                        byte[] hash;
                        using (var sha1 = new SHA1Managed())
                        {
                            hash = sha1.ComputeHash(stream);
                        }
                        stream.WriteByte(0x0);
                        stream.Write(hash, 0, 20);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static void WriteFileEntry(FileStream stream, FileEntry entry)
        {
            if (entry.OrgName != null)
                PboUtilities.WriteASIIZ(stream, entry.OrgName);
            else
                PboUtilities.WriteString(stream, entry.FileName);
            long packing = 0x0;
            switch (entry.PackingMethod)
            {
                case PackingType.Packed:
                    packing = 0x43707273;
                    break;
                case PackingType.Encrypted:
                    packing = 0x456e6372;
                    break;
            }
            PboUtilities.WriteLong(stream, packing);
            PboUtilities.WriteLong(stream, (long)entry.OriginalSize);
            PboUtilities.WriteLong(stream, (long)entry.StartOffset);
            PboUtilities.WriteLong(stream, (long)entry.TimeStamp);
            PboUtilities.WriteLong(stream, (long)entry.DataSize);
        }

        private static void WriteProductEntry(ProductEntry productEntry, FileStream stream)
        {
            PboUtilities.WriteString(stream, "sreV");
            stream.Write(new byte[15], 0, 15);
            if (!string.IsNullOrEmpty(productEntry.Prefix)) {
                PboUtilities.WriteString(stream, "prefix");
                PboUtilities.WriteString(stream, productEntry.Prefix);
            }
            else
                return;
            if (!string.IsNullOrEmpty(productEntry.ProductVersion))
            {
                PboUtilities.WriteString(stream, "version");
                PboUtilities.WriteString(stream, productEntry.ProductVersion);
            }
            else
                return;
            foreach (var additional in productEntry.Addtional)
            {
                PboUtilities.WriteString(stream, additional.Name);
                PboUtilities.WriteString(stream, additional.Value);
            }
        }

        public PboArchive(string path, bool close = true)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("File not Found");
            _path = path;
            _stream = new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,8,FileOptions.SequentialScan);
            if (_stream.ReadByte() != 0x0)
                return;
            if (!ReadHeader(_stream))
                _stream.Position = 0;
            while (true)
            {
                if (!ReadEntry(_stream))
                    break;
            }
            _dataStart = _stream.Position;
            ReadChecksum(_stream);
            if (close)
            {
                _stream.Dispose();
                _stream = null;
            }
            
        }

        private void ReadChecksum(FileStream stream)
        {
            var pos = DataStart + Files.Sum(fileEntry => (long)fileEntry.DataSize) + 1;
            stream.Position = pos;
            _checksum = new byte[20];
            stream.Read(Checksum, 0, 20);
            stream.Position = DataStart;
        }

        public List<FileEntry> Files
        {
            get { return _files; }
        }

        public ProductEntry ProductEntry
        {
            get { return _productEntry; }
        }

        public byte[] Checksum
        {
            get { return _checksum; }
        }

        public string PboPath
        {
            get { return _path; }
        }

        public long DataStart
        {
            get { return _dataStart; }
        }

        private bool ReadEntry(FileStream stream)
        {
            var file = PboUtilities.ReadStringArray(stream);
            var filename = Encoding.UTF8.GetString(file).Replace("\t","\\t");

            var packing = PboUtilities.ReadLong(stream);

            var size = PboUtilities.ReadLong(stream);

            var startOffset = PboUtilities.ReadLong(stream);

            var timestamp = PboUtilities.ReadLong(stream);
            var datasize = PboUtilities.ReadLong(stream);
            var entry = new FileEntry(this, filename, packing, size, timestamp, datasize, file, startOffset);
            if (entry.FileName == "")
            {
                entry.OrgName = new byte[0];
                return false;
            }
            Files.Add(entry);
            return true;
        }

        private bool ReadHeader(FileStream stream)
        {
            // TODO FIX SO BROKEN
            var str = PboUtilities.ReadString(stream);
            if (str != "sreV")
                return false;
            int count = 0;
            while (count < 15)
            {
                stream.ReadByte();
                count++;
            }
            var list = new List<Additional>();
            var pboname = "";
            var version = "";
            var prefix = PboUtilities.ReadString(stream);
            if (!string.IsNullOrEmpty(prefix))
            {
                pboname = PboUtilities.ReadString(stream);
                if (!string.IsNullOrEmpty(pboname))
                {
                    version = PboUtilities.ReadString(stream);

                    if (!string.IsNullOrEmpty(version))
                    {
                        version = PboUtilities.ReadString(stream);
                    }
                    while (stream.ReadByte() != 0x0)
                    {
                        stream.Position--;
                        var name = PboUtilities.ReadString(stream);
                        var value = PboUtilities.ReadString(stream);
                        list.Add(new Additional(name, value));
                    }
                }
            }
            _productEntry = new ProductEntry(prefix, pboname, version, list);

            return true;
        }

        public bool ExtractAll(string outpath)
        {
            if (!Directory.Exists(outpath))
                Directory.CreateDirectory(outpath);
                var buffer = new byte[10000000];
                int files = 0;
                foreach (var file in Files)
                {
                    var stream = GetFileStream(file);
                    
                    Console.WriteLine("FILE START");
                    files++;
                    long totalread = (long)file.DataSize;
                    var pboPath =
                        SterilizePath(System.IO.Path.Combine(outpath, file.FileName));
                    if (!Directory.Exists(System.IO.Path.GetDirectoryName(pboPath)))
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(pboPath));
                    using (var outfile = File.Create(pboPath))
                    {
                        while (totalread > 0)
                        {
                            var read = stream.Read(buffer, 0, (int)Math.Min(10000000, totalread));
                            if (read <= 0)
                                return true;
                            outfile.Write(buffer, 0, read);
                            totalread -= (long)read;
                        }
                    }
                    Console.WriteLine("FILE END " + files);
                }
            return true;
        }

        public bool Extract(FileEntry fileEntry, string outpath)
        {
            if(string.IsNullOrEmpty(outpath))
                throw new NullReferenceException("Is null or empty");
            Stream mem = GetFileStream(fileEntry);
            if (mem == null)
                throw new Exception("WTF no stream");
            if (!Directory.Exists(System.IO.Path.GetDirectoryName(outpath)))
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outpath));
            var totalread = fileEntry.DataSize;
            using (var outfile = File.OpenWrite(outpath))
            {
                var buffer = new byte[2949120];
                while (totalread > 0)
                {
                    var read = mem.Read(buffer, 0, (int)Math.Min(2949120, totalread));
                    outfile.Write(buffer, 0, read);
                    totalread -= (ulong)read;
                }
            }
            mem.Close();
            return true;
        }

        private Stream GetFileStream(FileEntry fileEntry)
        {
            if (_stream != null)
            {
                _stream.Position = (long)GetFileStreamPos(fileEntry);
                return _stream;
            }
            var mem = File.OpenRead(PboPath);
            mem.Position = (long)GetFileStreamPos(fileEntry);
            return mem;
        }

        private ulong GetFileStreamPos(FileEntry fileEntry)
        {

            var start = (ulong)DataStart;
            return Files.TakeWhile(entry => entry != fileEntry).Aggregate(start, (current, entry) => current + entry.DataSize);
        }




        
        // returns a stream
        /// <summary>
        /// Returns a filestream to the ENTIRE pbo set at the file entry pos.
        /// </summary>
        /// <param name="fileEntry"></param>
        /// <returns></returns>
        public Stream Extract(FileEntry fileEntry)
        {
            return GetFileStream(fileEntry);
        }

        public void Dispose()
        {
            _stream?.Dispose();
        }

        /// <summary>
        /// Return the files that should be added to the pbo
        /// </summary>
        /// <param name="folder"></param>
        /// <returns>Array with string</returns>
        public string[] GetFiles(string folder, Config.FilterFileConfig config = null)
        {
            if (config == null)
            {
                config = new Config.FilterFileConfig();
                config.DefaultFileConfig();
            }
            // Create a new empty List.
            List<string> files = new List<string>();

            // Find all hidden folders
            //List<string> hiddenFolders = new List<string>();
            IEnumerable<string> hiddenFolders = null;
            try
            {
                hiddenFolders = getHidden(folder);
            }
            catch (DirectoryNotFoundException)
            {
            }
            
            
            // Loop all files in folder to se what to export.
            foreach (string file in Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories))
            {
                FileInfo fileInfo = new FileInfo(file);

                // Filter what file to allow
                if (config.forbiddenExtension(fileInfo.Extension))
                {
                    continue;
                }
                if (config.forbiddenSubstringInPath(fileInfo.FullName))
                {
                    continue;
                }
                // ... hidden or in hidden folder
                if (config.ExcludeAllHidden)
                {
                    DirectoryInfo info = new DirectoryInfo(file);
                    if (
                        (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden
                        || hiddenFolders.Any(file.StartsWith)
                    )
                    {
                        continue;
                    }
                }
                
                // Add file to list
                files.Add(file);
            }

            return files.ToArray();
        }

        private List<string> getHidden(string path)
        {
            List<string> filtered = new List<string>();
            string[] directory = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
            
            foreach (string subDir in directory)
            {
                DirectoryInfo subDirInfo = new DirectoryInfo(subDir);
                if (subDirInfo.Attributes.HasFlag(FileAttributes.Hidden))
                {
                    filtered.Add(subDir);
                }
            }

            return filtered;
        }
    }
}