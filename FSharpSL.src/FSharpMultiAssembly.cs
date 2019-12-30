using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FSharpSL
{
    internal sealed class FSharpMultiAssembly
    {
        private readonly Dictionary<string, FSharpAssembly> Assemblies = new Dictionary<string, FSharpAssembly>();

        private FSharpMultiAssembly()
        {
        }

        private FSharpMultiAssembly(Dictionary<string, FSharpAssembly> assemblies)
        {
            Assemblies = assemblies;
        }

        internal FSharpMultiAssembly(IEnumerable<FSharpCompilerOptionsBuilder> builders)
        {
            foreach (var builder in builders)
            {
                Assemblies.Add(Path.GetFileNameWithoutExtension(builder.FileName), new FSharpAssembly(builder));
            }
        }

        public static FSharpMultiAssembly Combine(FSharpMultiAssembly assembly1, FSharpMultiAssembly assembly2)
        {
            var newasm = new FSharpMultiAssembly();

            foreach (var asm in assembly1.Assemblies)
            {
                newasm.Assemblies.Add(asm.Key, asm.Value);
            }

            foreach (var asm in assembly2.Assemblies)
            {
                newasm.Assemblies.Add(asm.Key, asm.Value);
            }

            return newasm;
        }

        public static FSharpMultiAssembly Combine(params FSharpMultiAssembly[] assemblies)
        {
            var newasm = new FSharpMultiAssembly();

            foreach (var combos in assemblies)
            {
                foreach (var asm in combos.Assemblies)
                {
                    newasm.Assemblies.Add(asm.Key, asm.Value);
                }
            }

            return newasm;
        }

        public static async Task<FSharpMultiAssembly> CreateAsync(IEnumerable<FSharpCompilerOptionsBuilder> builders)
        {
            var tasks = new Dictionary<string, Task<FSharpAssembly>>();

            foreach (var builder in builders)
            {
                tasks.Add(builder.FileName, FSharpAssembly.CreateAsync(builder));
            }

            await Task.WhenAll(tasks.Values).ConfigureAwait(false);

            var finishedAssemblies = new Dictionary<string, FSharpAssembly>();

            foreach (var asm in tasks)
            {
                finishedAssemblies.Add(Path.GetFileNameWithoutExtension(asm.Key), await asm.Value.ConfigureAwait(false));
            }

            return new FSharpMultiAssembly(finishedAssemblies);
        }

        public static FSharpMultiAssembly CreateFromDirectory(string directory)
        {
            return new FSharpMultiAssembly(
                Directory
                .EnumerateFiles(directory, "*.fsx")
                .Select(file => new FSharpCompilerOptionsBuilder(file)));
        }

        public T CreateDelegate<T>(string assembly, string method) where T : Delegate
        {
            return Assemblies[assembly].CreateDelegate<T>(method);
        }

        public Func<TResult> CreateFunction<TResult>(string assembly, string method)
        {
            return CreateDelegate<Func<TResult>>(assembly, method);
        }

        public Func<T1, TResult> CreateFunction<T1, TResult>(string assembly, string method)
        {
            return CreateDelegate<Func<T1, TResult>>(assembly, method);
        }

        public Func<T1, T2, TResult> CreateFunction<T1, T2, TResult>(string assembly, string method)
        {
            return CreateDelegate<Func<T1, T2, TResult>>(assembly, method);
        }

        public Func<T1, T2, T3, TResult> CreateFunction<T1, T2, T3, TResult>(string assembly, string method)
        {
            return CreateDelegate<Func<T1, T2, T3, TResult>>(assembly, method);
        }

        public Func<T1, T2, T3, T4, TResult> CreateFunction<T1, T2, T3, T4, TResult>(string assembly, string method)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, TResult>>(assembly, method);
        }

        public Func<T1, T2, T3, T4, T5, TResult> CreateFunction<T1, T2, T3, T4, T5, TResult>(string assembly, string method)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, TResult>>(assembly, method);
        }

        public Func<T1, T2, T3, T4, T5, T6, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, TResult>(string assembly, string method)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, TResult>>(assembly, method);
        }

        public Func<T1, T2, T3, T4, T5, T6, T7, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, T7, TResult>(string assembly, string method)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, TResult>>(assembly, method);
        }

        public Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(string assembly, string method)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>>(assembly, method);
        }

        public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(string assembly, string method)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>>(assembly, method);
        }

        public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(string assembly, string method)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>>(assembly, method);
        }

        public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(string assembly, string method)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>>(assembly, method);
        }

        public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(string assembly, string method)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>>(assembly, method);
        }

        public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(string assembly, string method)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>>(assembly, method);
        }

        public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(string assembly, string method)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>>(assembly, method);
        }

        public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(string assembly, string method)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>>(assembly, method);
        }

        public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(string assembly, string method)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>>(assembly, method);
        }

        public Action CreateAction(string assembly, string method)
        {
            return CreateDelegate<Action>(assembly, method);
        }

        public Action<T> CreateAction<T>(string assembly, string method)
        {
            return CreateDelegate<Action<T>>(assembly, method);
        }

        public Action<T1, T2> CreateAction<T1, T2>(string assembly, string method)
        {
            return CreateDelegate<Action<T1, T2>>(assembly, method);
        }

        public Action<T1, T2, T3> CreateAction<T1, T2, T3>(string assembly, string method)
        {
            return CreateDelegate<Action<T1, T2, T3>>(assembly, method);
        }

        public Action<T1, T2, T3, T4> CreateAction<T1, T2, T3, T4>(string assembly, string method)
        {
            return CreateDelegate<Action<T1, T2, T3, T4>>(assembly, method);
        }

        public Action<T1, T2, T3, T4, T5> CreateAction<T1, T2, T3, T4, T5>(string assembly, string method)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5>>(assembly, method);
        }

        public Action<T1, T2, T3, T4, T5, T6> CreateAction<T1, T2, T3, T4, T5, T6>(string assembly, string method)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6>>(assembly, method);
        }

        public Action<T1, T2, T3, T4, T5, T6, T7> CreateAction<T1, T2, T3, T4, T5, T6, T7>(string assembly, string method)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7>>(assembly, method);
        }

        public Action<T1, T2, T3, T4, T5, T6, T7, T8> CreateAction<T1, T2, T3, T4, T5, T6, T7, T8>(string assembly, string method)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7, T8>>(assembly, method);
        }

        public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> CreateAction<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string assembly, string method)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9>>(assembly, method);
        }

        public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> CreateAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string assembly, string method)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>(assembly, method);
        }

        public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> CreateAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string assembly, string method)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>(assembly, method);
        }

        public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> CreateAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string assembly, string method)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>(assembly, method);
        }

        public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> CreateAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string assembly, string method)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>(assembly, method);
        }

        public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> CreateAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string assembly, string method)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>(assembly, method);
        }

        public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> CreateAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string assembly, string method)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>(assembly, method);
        }

        public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> CreateAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string assembly, string method)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>>(assembly, method);
        }

        public IReadOnlyDictionary<string, FSharpAssembly> GetLoadedAssemblies()
        {
            return new ReadOnlyDictionary<string, FSharpAssembly>(Assemblies);
        }
    }
}
