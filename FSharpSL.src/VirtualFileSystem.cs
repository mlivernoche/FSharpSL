using FSharp.Compiler.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FSharpSL
{
    internal sealed class VirtualFileSystem : IFileSystem
    {
        private static IFileSystem Default { get; } = new DefaultFileSystem();

        private HashSet<string> ReferencePaths { get; } = new HashSet<string>();
        private Dictionary<string, byte[]> AllowedFiles { get; } = new Dictionary<string, byte[]>();
        private Dictionary<string, string> ExplicitReferencePaths { get; } = new Dictionary<string, string>();
        private Dictionary<string, string> ImplicitReferencePaths { get; } = new Dictionary<string, string>();
        private HashSet<string> LoadedAssemblies { get; } = new();

        internal IReadOnlyDictionary<string, string> GetExplicitlyLoadedReferences() => new ReadOnlyDictionary<string, string>(ExplicitReferencePaths);

        internal IReadOnlyDictionary<string, string> GetImplicitlyLoadedReferences() => new ReadOnlyDictionary<string, string>(ImplicitReferencePaths);

        internal VirtualFileSystem(IEnumerable<string> references)
        {
            ReferencePaths = new HashSet<string>(references);
        }

        internal VirtualFileSystem(IEnumerable<FSharpScript> scripts)
        {
            ReferencePaths = new HashSet<string>(scripts.SelectMany(static x => x.Builder.GetReferences()));
            AllowedFiles = scripts.ToDictionary(static x => x.Builder.FileName, static x => x.Script.ToArray());
        }

        internal void AddFile(string path, byte[] contents)
        {
            AllowedFiles.Add(path, contents);
        }

        internal void AddFile(FSharpScript script)
        {
            AllowedFiles.Add(script.Builder.FileName, script.Script.ToArray());
        }

        Assembly IFileSystem.AssemblyLoad(AssemblyName assemblyName)
        {
            LoadedAssemblies.Add(assemblyName.FullName);
            return Default.AssemblyLoad(assemblyName);
        }

        Assembly IFileSystem.AssemblyLoadFrom(string fileName)
        {
            throw new NotSupportedException();
        }

        void IFileSystem.FileDelete(string fileName)
        {
            throw new NotSupportedException();
        }

        Stream IFileSystem.FileStreamCreateShim(string fileName)
        {
            throw new NotSupportedException();
        }

        Stream IFileSystem.FileStreamReadShim(string fileName)
        {
            if (AllowedFiles.TryGetValue(fileName, out var bytes))
            {
                return new MemoryStream(bytes);
            }

            if(AllowedFiles.TryGetValue(Path.GetFileName(fileName), out var secondBytes))
            {
                return new MemoryStream(secondBytes);
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

        Stream IFileSystem.FileStreamWriteExistingShim(string fileName)
        {
            throw new NotSupportedException();
        }

        string IFileSystem.GetFullPathShim(string fileName)
        {
            return Default.GetFullPathShim(fileName);
        }

        DateTime IFileSystem.GetLastWriteTimeShim(string fileName)
        {
            return Default.GetLastWriteTimeShim(fileName);
        }

        string IFileSystem.GetTempPathShim()
        {
            return Default.GetTempPathShim();
        }

        bool IFileSystem.IsInvalidPathShim(string filename)
        {
            return Default.IsInvalidPathShim(filename);
        }

        bool IFileSystem.IsPathRootedShim(string path)
        {
            return Default.IsPathRootedShim(path);
        }

        bool IFileSystem.IsStableFileHeuristic(string fileName)
        {
            return Default.IsStableFileHeuristic(fileName);
        }

        byte[] IFileSystem.ReadAllBytesShim(string fileName)
        {
            if (AllowedFiles.TryGetValue(fileName, out var bytes) && bytes != null)
            {
                return bytes;
            }
            else if (AllowedFiles.TryGetValue(Path.GetFileName(fileName), out var secondBytes))
            {
                return secondBytes;
            }
            else if (ReferencePaths.Contains(fileName))
            {
                return Default.ReadAllBytesShim(fileName);
            }

            throw new NotSupportedException();
        }

        bool IFileSystem.SafeExists(string fileName)
        {
            if (AllowedFiles.ContainsKey(fileName))
            {
                return true;
            }
            else if (AllowedFiles.ContainsKey(Path.GetFileName(fileName)))
            {
                return true;
            }

            return Default.SafeExists(fileName);
        }
    }
}
