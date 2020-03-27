using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FSharpSL
{
    internal class FSharpScriptLoader
    {
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

        public virtual void ValidateAssembly(FSharpAssembly assembly)
        {
            var explicitRefs = VirtualFileSystem.GetExplicitlyLoadedReferences();
            var implicitRefs = VirtualFileSystem.GetImplicitlyLoadedReferences();
            var refs = assembly.GetReferencedAssemblies();

            foreach (var asm in refs)
            {
                var full = asm.FullName;
                if(!(explicitRefs.ContainsKey(full) || implicitRefs.ContainsKey(full) || asm.Name == "System.Private.CoreLib"))
                {
                    throw new Exception();
                }
            }
        }

        public virtual void ValidateAssemblies(FSharpMultiAssembly assemblies)
        {
            foreach(var asm in assemblies.GetLoadedAssemblies().Values)
            {
                ValidateAssembly(asm);
            }
        }
    }
}
