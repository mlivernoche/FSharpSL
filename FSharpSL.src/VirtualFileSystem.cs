using FSharp.Compiler.AbstractIL.Internal;
using System;
using System.Collections.Generic;
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
        private static readonly HashSet<string> RequiredAssemblies = new HashSet<string>
        {
            "mscorlib.dll",
            "System.Private.CoreLib.dll",
            "System.Runtime.dll",
            "System.Runtime.Extensions.dll",
            "FSharp.Core.dll",
            "System.Globalization.dll",
            "System.Collections.dll"
        };

        private HashSet<string> ReferencePaths { get; } = new HashSet<string>();
        private Dictionary<string, byte[]> AllowedFiles { get; } = new Dictionary<string, byte[]>();

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
            if (RequiredAssemblies.Contains(assemblyName.Name + ".dll"))
            {
                var asm = Default.AssemblyLoad(assemblyName);

                if (Path.GetDirectoryName(asm.Location) != Environment.CurrentDirectory)
                {
                    throw new NotSupportedException();
                }

                return asm;
            }
            else
            {
                foreach (var asm in ReferencePaths)
                {
                    var asmName = AssemblyName.GetAssemblyName(asm);
                    if (asmName.FullName == assemblyName.FullName)
                    {
                        return Default.AssemblyLoad(asmName);
                    }
                }
            }

            throw new NotSupportedException();
        }

        public Assembly AssemblyLoadFrom(string fileName)
        {
            if (ReferencePaths.Contains(fileName))
            {
                return Default.AssemblyLoadFrom(fileName);
            }

            throw new NotSupportedException();
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
            if (RequiredAssemblies.Contains(Path.GetFileName(fileName)))
            {
                if (Path.GetDirectoryName(fileName) != Environment.CurrentDirectory)
                {
                    throw new NotSupportedException();
                }
                else if (!File.Exists(fileName))
                {
                    throw new FileNotFoundException();
                }

                return Default.FileStreamReadShim(fileName);
            }
            else if (AllowedFiles.TryGetValue(fileName, out var bytes))
            {
                return new MemoryStream(bytes);
            }
            else if (ReferencePaths.Contains(fileName))
            {
                if (!File.Exists(fileName))
                {
                    throw new FileNotFoundException();
                }

                return Default.FileStreamReadShim(fileName);
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
