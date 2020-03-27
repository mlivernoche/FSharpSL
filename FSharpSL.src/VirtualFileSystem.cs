using FSharp.Compiler.AbstractIL.Internal;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace FSharpSL
{
    internal sealed class VirtualFileSystem : Library.Shim.IFileSystem, IDisposable
    {
        static VirtualFileSystem()
        {
            Library.Shim.FileSystem = Default;
        }

        private static readonly object FileSystemLock = new object();
        private static readonly Library.Shim.IFileSystem Default = new Library.Shim.DefaultFileSystem();

        private HashSet<string> ReferencePaths { get; } = new HashSet<string>();
        private Dictionary<string, byte[]> AllowedFiles { get; } = new Dictionary<string, byte[]>();
        private static Dictionary<string, string> ExplicitReferencePaths { get; } = new Dictionary<string, string>();
        private static Dictionary<string, string> ImplicitReferencePaths { get; } = new Dictionary<string, string>();

        public static IReadOnlyDictionary<string, string> GetExplicitlyLoadedReferences() => new ReadOnlyDictionary<string, string>(ExplicitReferencePaths);

        public static IReadOnlyDictionary<string, string> GetImplicitlyLoadedReferences() => new ReadOnlyDictionary<string, string>(ImplicitReferencePaths);

        internal VirtualFileSystem()
        {
            lock (FileSystemLock)
            {
                if (Library.Shim.FileSystem != Default)
                {
                    throw new NotSupportedException("Cannot have multiple custom IFileSystem instances.");
                }

                Library.Shim.FileSystem = this;
            }
        }

        internal VirtualFileSystem(IEnumerable<string> references) : this()
        {
            ReferencePaths = new HashSet<string>(references);
        }

        public void AddFile(string path, byte[] contents)
        {
            AllowedFiles.Add(path, contents);
        }

        public Assembly AssemblyLoad(AssemblyName assemblyName)
        {
            return Default.AssemblyLoad(assemblyName);
        }

        public Assembly AssemblyLoadFrom(string fileName)
        {
            return Default.AssemblyLoadFrom(fileName);
        }

        public void FileDelete(string fileName)
        {
            throw new NotSupportedException();
        }

        public Stream FileStreamCreateShim(string fileName)
        {
            throw new NotSupportedException();
        }

        public Stream FileStreamReadShim(string fileName)
        {
            if (AllowedFiles.TryGetValue(fileName, out var bytes))
            {
                return new MemoryStream(bytes);
            }

            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException();
            }

            // if this is an assembly, AssemblyName.GetAssemblyName will work.
            try
            {
                var name = AssemblyName.GetAssemblyName(fileName);
                var assemblyMap = ReferencePaths.Contains(fileName) ? ExplicitReferencePaths : ImplicitReferencePaths;

                if (assemblyMap.TryGetValue(name.FullName, out var path))
                {
                    if (path != fileName)
                    {
                        throw new NotSupportedException("Trying to load the same assembly from different locations.");
                    }
                }
                else
                {
                    assemblyMap.Add(name.FullName, fileName);
                }

                return Default.FileStreamReadShim(fileName);
            }
            catch (BadImageFormatException)
            {
                throw new NotSupportedException("Cannot load a non-assembly file.");
            }

            throw new NotSupportedException();
        }

        public Stream FileStreamWriteExistingShim(string fileName)
        {
            throw new NotSupportedException();
        }

        public string GetFullPathShim(string fileName)
        {
            return Default.GetFullPathShim(fileName);
        }

        public DateTime GetLastWriteTimeShim(string fileName)
        {
            return Default.GetLastWriteTimeShim(fileName);
        }

        public string GetTempPathShim()
        {
            return Default.GetTempPathShim();
        }

        public bool IsInvalidPathShim(string filename)
        {
            return Default.IsInvalidPathShim(filename);
        }

        public bool IsPathRootedShim(string path)
        {
            return Default.IsPathRootedShim(path);
        }

        public bool IsStableFileHeuristic(string fileName)
        {
            return Default.IsStableFileHeuristic(fileName);
        }

        public byte[] ReadAllBytesShim(string fileName)
        {
            if (AllowedFiles.TryGetValue(fileName, out var bytes) && bytes != null)
            {
                return bytes;
            }
            else if (ReferencePaths.Contains(fileName))
            {
                return Default.ReadAllBytesShim(fileName);
            }

            throw new NotSupportedException();
        }

        public bool SafeExists(string fileName)
        {
            return Default.SafeExists(fileName);
        }

        private bool disposedValue = false;

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Library.Shim.FileSystem = Default;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
