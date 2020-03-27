using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSharpSL
{
    internal static class FSharpAssemblyBuilder
    {
        public static FSharpAssembly Build(FSharpCompilerOptionsBuilder optionsBuilder)
        {
            using var vfs = new VirtualFileSystem(optionsBuilder.GetReferences());
            vfs.AddFile(optionsBuilder.FileName, File.ReadAllBytes(optionsBuilder.FileName));
            return new FSharpAssembly(optionsBuilder);
        }

        public static async Task<FSharpAssembly> BuildAsync(FSharpCompilerOptionsBuilder optionsBuilder, CancellationToken token)
        {
            using var vfs = new VirtualFileSystem(optionsBuilder.GetReferences());
            vfs.AddFile(optionsBuilder.FileName, await File.ReadAllBytesAsync(optionsBuilder.FileName, token).ConfigureAwait(false));
            return await FSharpAssembly.CreateAsync(optionsBuilder, token).ConfigureAwait(false);
        }

        public static async Task<FSharpAssembly> BuildAsync(FSharpCompilerOptionsBuilder optionsBuilder)
        {
            return await BuildAsync(optionsBuilder, CancellationToken.None);
        }

        public static FSharpAssembly Build(FSharpCompilerOptionsBuilder optionsBuilder, FSharpScriptLoader loader)
        {
            using var vfs = new VirtualFileSystem(optionsBuilder.GetReferences());
            vfs.AddFile(optionsBuilder.FileName, loader.Load(optionsBuilder.FileName));
            var asm = new FSharpAssembly(optionsBuilder);
            loader.ValidateAssembly(asm);
            return asm;
        }

        public static async Task<FSharpAssembly> BuildAsync(FSharpCompilerOptionsBuilder optionsBuilder, FSharpScriptLoader loader, CancellationToken token)
        {
            using var vfs = new VirtualFileSystem(optionsBuilder.GetReferences());
            vfs.AddFile(optionsBuilder.FileName, await loader.LoadAsync(optionsBuilder.FileName, token).ConfigureAwait(false));
            var asm = await FSharpAssembly.CreateAsync(optionsBuilder, token).ConfigureAwait(false);
            loader.ValidateAssembly(asm);
            return asm;
        }

        public static async Task<FSharpAssembly> BuildAsync(FSharpCompilerOptionsBuilder optionsBuilder, FSharpScriptLoader loader)
        {
            return await BuildAsync(optionsBuilder, loader, CancellationToken.None);
        }

        public static FSharpMultiAssembly Build(IEnumerable<FSharpCompilerOptionsBuilder> optionBuilders)
        {
            using var vfs = new VirtualFileSystem(optionBuilders.SelectMany(builder => builder.GetReferences()).ToHashSet());

            foreach (var builder in optionBuilders)
            {
                vfs.AddFile(builder.FileName, File.ReadAllBytes(builder.FileName));
            }

            return new FSharpMultiAssembly(optionBuilders);
        }

        public static async Task<FSharpMultiAssembly> BuildAsync(IEnumerable<FSharpCompilerOptionsBuilder> optionBuilders, CancellationToken token)
        {
            using var vfs = new VirtualFileSystem(optionBuilders.SelectMany(builder => builder.GetReferences()).ToHashSet());
            var fileMap = new Dictionary<string, Task<byte[]>>();

            foreach (var build in optionBuilders)
            {
                fileMap.Add(build.FileName, File.ReadAllBytesAsync(build.FileName, token));
            }

            await Task.WhenAll(fileMap.Values).ConfigureAwait(false);

            foreach (var file in fileMap)
            {
                vfs.AddFile(file.Key, await file.Value.ConfigureAwait(false));
            }

            return await FSharpMultiAssembly.CreateAsync(optionBuilders, token).ConfigureAwait(false);
        }

        public static async Task<FSharpMultiAssembly> BuildAsync(IEnumerable<FSharpCompilerOptionsBuilder> optionBuilders)
        {
            return await BuildAsync(optionBuilders, CancellationToken.None);
        }

        public static FSharpMultiAssembly Build(IEnumerable<FSharpCompilerOptionsBuilder> optionBuilders, FSharpScriptLoader loader)
        {
            using var vfs = new VirtualFileSystem(optionBuilders.SelectMany(builder => builder.GetReferences()).ToHashSet());

            foreach (var builder in optionBuilders)
            {
                vfs.AddFile(builder.FileName, loader.Load(builder.FileName));
            }

            var asm = new FSharpMultiAssembly(optionBuilders);
            loader.ValidateAssemblies(asm);
            return asm;
        }

        public static async Task<FSharpMultiAssembly> BuildAsync(IEnumerable<FSharpCompilerOptionsBuilder> optionBuilders, FSharpScriptLoader loader, CancellationToken token)
        {
            using var vfs = new VirtualFileSystem(optionBuilders.SelectMany(builder => builder.GetReferences()).ToHashSet());
            var fileMap = new Dictionary<string, Task<byte[]>>();

            foreach (var builder in optionBuilders)
            {
                fileMap.Add(builder.FileName, loader.LoadAsync(builder.FileName, token));
            }

            await Task.WhenAll(fileMap.Values).ConfigureAwait(false);

            foreach (var path in fileMap)
            {
                vfs.AddFile(path.Key, await path.Value.ConfigureAwait(false));
            }

            var asm = await FSharpMultiAssembly.CreateAsync(optionBuilders, token).ConfigureAwait(false);
            loader.ValidateAssemblies(asm);
            return asm;
        }

        public static async Task<FSharpMultiAssembly> BuildAsync(IEnumerable<FSharpCompilerOptionsBuilder> optionBuilders, FSharpScriptLoader loader)
        {
            return await BuildAsync(optionBuilders, CancellationToken.None);
        }
    }
}
