using FSharp.Compiler;
using FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSharpSL
{
    internal sealed class FSharpAssembly
    {
        private static readonly FSharpChecker DefaultChecker = FSharpChecker.Create(
            FSharpOption<int>.None, FSharpOption<bool>.None, FSharpOption<bool>.None, FSharpOption<LegacyReferenceResolver>.None,
            FSharpOption<FSharpFunc<Tuple<string, DateTime>, FSharpOption<Tuple<object, IntPtr, int>>>>.None,
            FSharpOption<bool>.None, FSharpOption<bool>.None, FSharpOption<bool>.None, FSharpOption<bool>.None);

        private readonly Assembly _assembly;

        public string AssemblyFullName { get; }
        public string AssemblyName { get; }
        public AssemblyName[] GetReferencedAssemblies() => _assembly.GetReferencedAssemblies();

        private FSharpAssembly(Assembly asm)
        {
            _assembly = asm;
            AssemblyFullName = asm.GetName().FullName;
            AssemblyName = asm.GetName().Name ?? string.Empty;
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
            AssemblyName = _assembly.GetName().Name ?? string.Empty;
        }

        internal FSharpAssembly(FSharpCompilerOptionsBuilder builder) : this(builder, DefaultChecker)
        {
        }

        internal FSharpAssembly(FSharpCompilerOptionsBuilder builder, IEnumerable<string> references) : this(builder, DefaultChecker)
        {
        }

        internal static async Task<FSharpAssembly> CreateAsync(FSharpCompilerOptionsBuilder builder, FSharpChecker checker, CancellationToken token)
        {
            try
            {
                var args = builder.ToArray();

                var task = checker.CompileToDynamicAssembly(
                    args,
                    FSharpOption<Tuple<TextWriter, TextWriter>>.None,
                    FSharpOption<string>.None);

                var result = await FSharpAsync.StartAsTask(task, default, token).ConfigureAwait(false);

                if (result == null || result.Item3 == null)
                {
                    ThrowErrorMessages(builder.FileName, result.Item1);
                }

                return new FSharpAssembly(result.Item3.Value);
            }
            catch (TaskCanceledException ex)
            {
#if NETSTANDARD2_0
                throw;
#elif NET5_0
                // Workaround for https://github.com/dotnet/fsharp/issues/3219
                // ex.CancellationToken is not the same as token, for some reason.
                throw new TaskCanceledException(ex.Message, ex, token);
#endif
            }
        }

        internal static async Task<FSharpAssembly> CreateAsync(FSharpCompilerOptionsBuilder builder, FSharpChecker checker)
        {
            return await CreateAsync(builder, checker, CancellationToken.None).ConfigureAwait(false);
        }

        internal static async Task<FSharpAssembly> CreateAsync(FSharpCompilerOptionsBuilder builder, CancellationToken token)
        {
            return await CreateAsync(builder, DefaultChecker, token).ConfigureAwait(false);
        }

        internal static async Task<FSharpAssembly> CreateAsync(FSharpCompilerOptionsBuilder builder)
        {
            return await CreateAsync(builder, CancellationToken.None).ConfigureAwait(false);
        }

        private static void ThrowErrorMessages(string path, IEnumerable<FSharpDiagnostic> errors)
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
            error.AppendLine($"Number of Errors: {numberOfErrors.ToString("N0", CultureInfo.InvariantCulture)}");
            error.AppendLine($"Number of warnings: {numberOfWarnings.ToString("N0", CultureInfo.InvariantCulture)}");

            var errorMessages = new List<string>();
            var warningMessages = new List<string>();

            foreach (var err in errors)
            {
                if (err.Severity.IsError)
                {
                    errorMessages.Add($"Error #{err.ErrorNumber.ToString(CultureInfo.InvariantCulture)}, Line #{err.Range.StartLine.ToString(CultureInfo.InvariantCulture)}: {err.Message}");
                }
                else if (err.Severity.IsWarning)
                {
                    warningMessages.Add($"Warning #{err.ErrorNumber.ToString(CultureInfo.InvariantCulture)}, Line #{err.Range.StartLine.ToString(CultureInfo.InvariantCulture)}: {err.Message}");
                }
            }

            error.AppendLine();
            error.AppendLine($"========== ERRORS: {numberOfErrors.ToString("N0", CultureInfo.InvariantCulture)} ==========");

            foreach (var msg in errorMessages)
            {
                error.AppendLine(msg);
            }

            error.AppendLine();
            error.AppendLine($"========== WARNINGS: {numberOfWarnings.ToString("N0", CultureInfo.InvariantCulture)} ==========");

            foreach (var msg in warningMessages)
            {
                error.AppendLine(msg);
            }

            error.AppendLine(new string('+', 50));

            var e = new Exception(error.ToString());

            foreach (var err in errors)
            {
                e.Data.Add(Guid.NewGuid().ToString(), err.Message);
            }

            throw e;
        }

        private MethodInfo GetMethod(string methodName)
        {
            var t = _assembly.GetType(AssemblyName, false, true);

            if (t == null)
            {
                throw new TypeLoadException($"{AssemblyName} assembly not found.");
            }

            var method = t.GetMethod(methodName);

            if (method == null)
            {
                throw new TypeLoadException($"{methodName} method not found.");
            }

            if (!method.IsStatic)
            {
                throw new InvalidOperationException($"{AssemblyName}, {methodName}: cannot create a delegate instance of a non-static method (MethodBase.IsStatic={method.IsStatic.ToString()}).");
            }

            if (method.IsSpecialName)
            {
                throw new InvalidOperationException($"{AssemblyName}, {methodName}: cannot use a method with a special name (MethodBase.IsSpecialName={method.IsSpecialName.ToString()}).");
            }

            if (method.IsPrivate)
            {
                throw new InvalidOperationException($"{AssemblyName}, {methodName}: cannot use a private method (MethodBase.IsPrivate={method.IsPrivate.ToString()}).");
            }

            if (method.IsAbstract)
            {
                throw new InvalidOperationException($"{AssemblyName}, {methodName}: cannot use an abstract method with no implementation (MethodBase.IsAbstract={method.IsAbstract.ToString()}).");
            }

            if (method.IsConstructor)
            {
                throw new InvalidOperationException($"{AssemblyName}, {methodName}: cannot use a constructor (MethodBase.IsConstructor={method.IsConstructor.ToString()}).");
            }

            return method;
        }

        public T CreateDelegate<T>(string methodName) where T : Delegate
        {
            try
            {
                var method = GetMethod(methodName);
                var del = (T)method.CreateDelegate(typeof(T));
                return del;
            }
            catch (ArgumentException ex)
            {
                ex.Data.Add(Guid.NewGuid().ToString(), $"{AssemblyName}, {methodName}: failed to create method.");
                throw;
            }
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
