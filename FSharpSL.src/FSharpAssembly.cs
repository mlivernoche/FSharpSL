using FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FSharpSL
{
    internal sealed class FSharpAssembly
    {
        private static readonly FSharpChecker DefaultChecker = FSharpChecker.Create(default, default, default, default, default, default);

        private readonly Assembly _assembly;

        public string AssemblyFullName { get; }
        public string AssemblyName { get; }

        private FSharpAssembly(Assembly asm)
        {
            _assembly = asm;
            AssemblyFullName = asm.GetName().FullName;
            AssemblyName = asm.GetName().Name;
        }

        internal FSharpAssembly(FSharpCompilerOptionsBuilder builder, FSharpChecker checker)
        {
            var args = builder.ToArray();

            var task = checker.CompileToDynamicAssembly(
                args,
                FSharpOption<Tuple<TextWriter, TextWriter>>.None,
                FSharpOption<string>.None);

            var result = FSharpAsync.RunSynchronously(task, default, default);

            if (result.Item3 == null)
            {
                ThrowErrorMessages(builder.FileName, result.Item1);
            }

            var fsharpAssembly = result.Item3.Value;

            _assembly = fsharpAssembly;
            AssemblyFullName = _assembly.GetName().FullName;
            AssemblyName = _assembly.GetName().Name;
        }

        internal FSharpAssembly(FSharpCompilerOptionsBuilder builder) : this(builder, DefaultChecker)
        {
        }

        internal FSharpAssembly(FSharpCompilerOptionsBuilder builder, IEnumerable<string> references) : this(builder, DefaultChecker)
        {
        }

        internal static async Task<FSharpAssembly> CreateAsync(FSharpCompilerOptionsBuilder builder, FSharpChecker checker)
        {
            var args = builder.ToArray();

            var task = checker.CompileToDynamicAssembly(
                args,
                FSharpOption<Tuple<TextWriter, TextWriter>>.None,
                FSharpOption<string>.None);

            var result = await FSharpAsync.StartAsTask(task, default, default).ConfigureAwait(false);

            if (result.Item3 == null)
            {
                ThrowErrorMessages(builder.FileName, result.Item1);
            }

            return new FSharpAssembly(result.Item3.Value);
        }

        internal static async Task<FSharpAssembly> CreateAsync(FSharpCompilerOptionsBuilder builder)
        {
            return await CreateAsync(builder, DefaultChecker).ConfigureAwait(false);
        }

        private static void ThrowErrorMessages(string path, IEnumerable<FSharpErrorInfo> errors)
        {
            int numberOfErrors = 0;
            int numberOfWarnings = 0;
            var error = new StringBuilder();
            error.AppendLine(new string('+', 50));
            error.AppendLine("F# compiler returned errors (errors also put in Exception's Data property):");
            error.AppendLine($"Path: {path}");

            foreach (var err in errors)
            {
                if (err.Severity.IsError) numberOfErrors++;
                else if (err.Severity.IsWarning) numberOfWarnings++;
            }

            error.AppendLine();
            error.AppendLine($"Number of Errors: {numberOfErrors.ToString("N0")}");
            error.AppendLine($"Number of warnings: {numberOfWarnings.ToString("N0")}");

            var errorMessages = new List<string>();
            var warningMessages = new List<string>();

            foreach (var err in errors)
            {
                if (err.Severity.IsError)
                {
                    errorMessages.Add($"Error #{err.ErrorNumber.ToString()}, Line #{err.StartLineAlternate.ToString()}: {err.Message}");
                }
                else if (err.Severity.IsWarning)
                {
                    warningMessages.Add($"Warning #{err.ErrorNumber.ToString()}, Line #{err.StartLineAlternate.ToString()}: {err.Message}");
                }
            }

            error.AppendLine();
            error.AppendLine($"========== ERRORS: {numberOfErrors.ToString("N0")} ==========");

            foreach (var msg in errorMessages)
            {
                error.AppendLine(msg);
            }

            error.AppendLine();
            error.AppendLine($"========== WARNINGS: {numberOfWarnings.ToString("N0")} ==========");

            foreach (var msg in warningMessages)
            {
                error.AppendLine(msg);
            }

            var e = new Exception(error.ToString());

            foreach (var err in errors)
            {
                e.Data.Add(Guid.NewGuid().ToString(), err.Message);
            }

            error.AppendLine(new string('+', 50));
            throw e;
        }

        public IEnumerable<MethodInfo> GetMethods()
        {
            var t = _assembly.GetType(AssemblyName, false, true);
            return t.GetMethods();
        }

        private MethodInfo GetMethod(string methodName)
        {
            var t = _assembly.GetType(AssemblyName, false, true);

            var method = t.GetMethod(methodName);

            if (!method.IsStatic)
            {
                throw new InvalidOperationException("Cannot create a delegate instance of a non-static method.");
            }

            return method;
        }

        public T CreateDelegate<T>(string methodName) where T : Delegate
        {
            var method = GetMethod(methodName);
            var del = (T)method.CreateDelegate(typeof(T));
            return del;
        }

        public Func<TResult> CreateFunction<TResult>(string methodName)
        {
            return CreateDelegate<Func<TResult>>(methodName);
        }

        public Func<T1, TResult> CreateFunction<T1, TResult>(string methodName)
        {
            return CreateDelegate<Func<T1, TResult>>(methodName);
        }

        public Func<T1, T2, TResult> CreateFunction<T1, T2, TResult>(string methodName)
        {
            return CreateDelegate<Func<T1, T2, TResult>>(methodName);
        }

        public Func<T1, T2, T3, TResult> CreateFunction<T1, T2, T3, TResult>(string methodName)
        {
            return CreateDelegate<Func<T1, T2, T3, TResult>>(methodName);
        }

        public Func<T1, T2, T3, T4, TResult> CreateFunction<T1, T2, T3, T4, TResult>(string methodName)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, TResult>>(methodName);
        }

        public Func<T1, T2, T3, T4, T5, TResult> CreateFunction<T1, T2, T3, T4, T5, TResult>(string methodName)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, TResult>>(methodName);
        }

        public Func<T1, T2, T3, T4, T5, T6, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, TResult>(string methodName)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, TResult>>(methodName);
        }

        public Func<T1, T2, T3, T4, T5, T6, T7, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, T7, TResult>(string methodName)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, TResult>>(methodName);
        }

        public Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(string methodName)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>>(methodName);
        }

        public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(string methodName)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>>(methodName);
        }

        public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(string methodName)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>>(methodName);
        }

        public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(string methodName)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>>(methodName);
        }

        public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(string methodName)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>>(methodName);
        }

        public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(string methodName)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>>(methodName);
        }

        public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(string methodName)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>>(methodName);
        }

        public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(string methodName)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>>(methodName);
        }

        public Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(string methodName)
        {
            return CreateDelegate<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>>(methodName);
        }

        public Action CreateAction(string methodName)
        {
            return CreateDelegate<Action>(methodName);
        }

        public Action<T> CreateAction<T>(string methodName)
        {
            return CreateDelegate<Action<T>>(methodName);
        }

        public Action<T1, T2> CreateAction<T1, T2>(string methodName)
        {
            return CreateDelegate<Action<T1, T2>>(methodName);
        }

        public Action<T1, T2, T3> CreateAction<T1, T2, T3>(string methodName)
        {
            return CreateDelegate<Action<T1, T2, T3>>(methodName);
        }

        public Action<T1, T2, T3, T4> CreateAction<T1, T2, T3, T4>(string methodName)
        {
            return CreateDelegate<Action<T1, T2, T3, T4>>(methodName);
        }

        public Action<T1, T2, T3, T4, T5> CreateAction<T1, T2, T3, T4, T5>(string methodName)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5>>(methodName);
        }

        public Action<T1, T2, T3, T4, T5, T6> CreateAction<T1, T2, T3, T4, T5, T6>(string methodName)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6>>(methodName);
        }

        public Action<T1, T2, T3, T4, T5, T6, T7> CreateAction<T1, T2, T3, T4, T5, T6, T7>(string methodName)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7>>(methodName);
        }

        public Action<T1, T2, T3, T4, T5, T6, T7, T8> CreateAction<T1, T2, T3, T4, T5, T6, T7, T8>(string methodName)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7, T8>>(methodName);
        }

        public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> CreateAction<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string methodName)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9>>(methodName);
        }

        public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> CreateAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string methodName)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>(methodName);
        }

        public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> CreateAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string methodName)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>(methodName);
        }

        public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> CreateAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string methodName)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>(methodName);
        }

        public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> CreateAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string methodName)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>(methodName);
        }

        public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> CreateAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string methodName)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>(methodName);
        }

        public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> CreateAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string methodName)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>(methodName);
        }

        public Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> CreateAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string methodName)
        {
            return CreateDelegate<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>>(methodName);
        }
    }
}
