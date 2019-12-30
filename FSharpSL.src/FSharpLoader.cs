using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FSharpSE
{
    public static class FSharpLoader
    {
        public static FSharpAssembly Load(FSharpCompilerOptionsBuilder builder)
        {
            using var vfs = new VirtualFileSystem();
            vfs.AddFile(builder.FileName, File.ReadAllBytes(builder.FileName));
            return new FSharpAssembly(builder);
        }

        public static async Task<FSharpAssembly> LoadAsync(FSharpCompilerOptionsBuilder builder)
        {
            using var vfs = new VirtualFileSystem();
            vfs.AddFile(builder.FileName, await File.ReadAllBytesAsync(builder.FileName).ConfigureAwait(false));
            return await FSharpAssembly.CreateAsync(builder).ConfigureAwait(false);
        }

        public static FSharpAssembly Load(FSharpCompilerOptionsBuilder builder, FSharpScriptLoader loader)
        {
            using var vfs = new VirtualFileSystem();
            vfs.AddFile(builder.FileName, loader.Load(builder.FileName));
            return new FSharpAssembly(builder);
        }

        public static async Task<FSharpAssembly> LoadAsync(FSharpCompilerOptionsBuilder builder, FSharpScriptLoader loader)
        {
            using var vfs = new VirtualFileSystem();
            vfs.AddFile(builder.FileName, await loader.LoadAsync(builder.FileName).ConfigureAwait(false));
            return await FSharpAssembly.CreateAsync(builder).ConfigureAwait(false);
        }

        public static FSharpAssembly Load(FSharpCompilerOptionsBuilder builder, FSharpScriptLoader loader, IEnumerable<string> references)
        {
            using var vfs = new VirtualFileSystem(references);
            vfs.AddFile(builder.FileName, loader.Load(builder.FileName));
            return new FSharpAssembly(builder);
        }

        public static async Task<FSharpAssembly> LoadAsync(FSharpCompilerOptionsBuilder builder, FSharpScriptLoader loader, IEnumerable<string> references)
        {
            using var vfs = new VirtualFileSystem(references);
            vfs.AddFile(builder.FileName, await loader.LoadAsync(builder.FileName).ConfigureAwait(false));
            return await FSharpAssembly.CreateAsync(builder).ConfigureAwait(false);
        }

        public static FSharpMultiAssembly Load(IEnumerable<FSharpCompilerOptionsBuilder> builders)
        {
            using var vfs = new VirtualFileSystem();

            foreach (var builder in builders)
            {
                vfs.AddFile(builder.FileName, File.ReadAllBytes(builder.FileName));
            }

            return new FSharpMultiAssembly(builders);
        }

        public static async Task<FSharpMultiAssembly> LoadAsync(IEnumerable<FSharpCompilerOptionsBuilder> builders)
        {
            using var vfs = new VirtualFileSystem();
            var fileMap = new Dictionary<string, Task<byte[]>>();

            foreach (var build in builders)
            {
                fileMap.Add(build.FileName, File.ReadAllBytesAsync(build.FileName));
            }

            await Task.WhenAll(fileMap.Values).ConfigureAwait(false);

            foreach (var file in fileMap)
            {
                vfs.AddFile(file.Key, await file.Value.ConfigureAwait(false));
            }

            return await FSharpMultiAssembly.CreateAsync(builders).ConfigureAwait(false);
        }

        public static FSharpMultiAssembly Load(IEnumerable<FSharpCompilerOptionsBuilder> builders, FSharpScriptLoader loader)
        {
            using var vfs = new VirtualFileSystem();

            foreach (var builder in builders)
            {
                vfs.AddFile(builder.FileName, loader.Load(builder.FileName));
            }

            return new FSharpMultiAssembly(builders);
        }

        public static async Task<FSharpMultiAssembly> LoadAsync(IEnumerable<FSharpCompilerOptionsBuilder> builders, FSharpScriptLoader loader)
        {
            using var vfs = new VirtualFileSystem();
            var fileMap = new Dictionary<string, Task<byte[]>>();

            foreach (var builder in builders)
            {
                fileMap.Add(builder.FileName, loader.LoadAsync(builder.FileName));
            }

            await Task.WhenAll(fileMap.Values).ConfigureAwait(false);

            foreach (var path in fileMap)
            {
                vfs.AddFile(path.Key, await path.Value.ConfigureAwait(false));
            }

            return await FSharpMultiAssembly.CreateAsync(builders).ConfigureAwait(false);
        }
    }
}
