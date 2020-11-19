using FSharp.Compiler.AbstractIL.Internal;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSharpSL
{
    internal class FSharpScriptLoader
    {
        private HashSet<FSharpCompilerOptionsBuilder> CompilerOptions { get; }
        public IReadOnlyCollection<FSharpCompilerOptionsBuilder> Options => CompilerOptions;

        public VirtualFileSystem FileSystem { get; }

        protected FSharpScriptLoader(IEnumerable<FSharpCompilerOptionsBuilder> optionBuilders)
        {
            CompilerOptions = optionBuilders.ToHashSet();
            FileSystem = new VirtualFileSystem(CompilerOptions.SelectMany(x => x.GetReferences().ToHashSet()));
        }

        protected void AddFile(string path)
        {
            FileSystem.AddFile(path, Load(path));
        }

        protected async Task AddFileAsync(string path)
        {
            FileSystem.AddFile(path, await LoadAsync(path).ConfigureAwait(false));
        }

        public virtual byte[] Load(string filePath)
        {
            return File.ReadAllBytes(filePath);
        }

        public virtual async Task<byte[]> LoadAsync(string filePath)
        {
            return await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);
        }

        public virtual async Task<byte[]> LoadAsync(string filePath, CancellationToken token)
        {
            return await File.ReadAllBytesAsync(filePath, token).ConfigureAwait(false);
        }

        public FSharpMultiAssembly Build()
        {
            return new FSharpMultiAssembly(CompilerOptions);
        }

        public virtual void ValidateAssembly(FSharpAssembly assembly)
        {
            var explicitRefs = FileSystem.GetExplicitlyLoadedReferences();
            var implicitRefs = FileSystem.GetImplicitlyLoadedReferences();
            var refs = assembly.GetReferencedAssemblies();

            var entryRefs = (Assembly.GetEntryAssembly()?.GetReferencedAssemblies() ?? Array.Empty<AssemblyName>()).ToDictionary(x => x.FullName);
            var mainRefs = Assembly.GetExecutingAssembly().GetReferencedAssemblies().ToDictionary(x => x.FullName);
            var callingRefs = Assembly.GetCallingAssembly().GetReferencedAssemblies().ToDictionary(x => x.FullName);

            foreach (var asm in refs)
            {
                var full = asm.FullName;

                // this is by adding an assembly reference to the fsc build command.
                var isExplicitlyRefed = explicitRefs.ContainsKey(full);

                // this is typically done by the FSC itself.
                var isImplicitlyRefed = implicitRefs.ContainsKey(full);

                // this is done by the project build.
                var isBuiltRef = entryRefs.ContainsKey(full) ||
                    mainRefs.ContainsKey(full) ||
                    callingRefs.ContainsKey(full);

                // this is what .NET needs to run.
                var isCorelib = asm.Name == "System.Private.CoreLib";

                var okAssembly = isExplicitlyRefed || isImplicitlyRefed || isBuiltRef || isCorelib;

                if (!okAssembly)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"Cannot load assembly {asm.FullName} from {asm.CodeBase}.");
                    sb.AppendLine($"isExplicitlyRefed = {isExplicitlyRefed.ToString()}");
                    sb.AppendLine($"isImplicitlyRefed = {isImplicitlyRefed.ToString()}");
                    sb.AppendLine($"isBuiltRef = {isBuiltRef.ToString()}");
                    sb.AppendLine($"isCorelib = {isCorelib.ToString()}");
                    sb.AppendLine($"okAssembly = {okAssembly.ToString()}");
                    sb.AppendLine();

                    sb.AppendLine("Explicit References:");
                    foreach (var ex in explicitRefs)
                    {
                        sb.AppendLine($"{ex.Key}; {ex.Value}");
                    }
                    sb.AppendLine();

                    sb.AppendLine("Implicit References:");
                    foreach (var ex in implicitRefs)
                    {
                        sb.AppendLine($"{ex.Key}; {ex.Value}");
                    }
                    sb.AppendLine();

                    sb.AppendLine("Build References:");
                    foreach (var ex in entryRefs)
                    {
                        sb.AppendLine($"{ex.Key}; {ex.Value.FullName}");
                    }
                    sb.AppendLine();

                    throw new Exception(sb.ToString());
                }
            }
        }

        public virtual void ValidateAssemblies(FSharpMultiAssembly assemblies)
        {
            foreach (var asm in assemblies.GetLoadedAssemblies().Values)
            {
                ValidateAssembly(asm);
            }
        }
    }
}
