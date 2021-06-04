using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FSharpSL
{
    internal sealed class FSharpCompilerOptionsBuilder : IEnumerable<string>
    {
        private List<string> Commands { get; } = new List<string>();
        private HashSet<string> Uniques { get; } = new HashSet<string>();
        private HashSet<string> References { get; } = new HashSet<string>();
        public string FileName { get; }
        public string AssemblyName { get; }

        public FSharpCompilerOptionsBuilder(string fileName, string assemblyName)
        {
            FileName = fileName;
            AssemblyName = assemblyName;
            Add("fsc.exe");
            Add("-a");
            Add(fileName);
        }

        public void Add(string command)
        {
            if(Uniques.Add(command))
            {
                Commands.Add(command);

                var span = command.AsSpan();
#if NETSTANDARD2_0
                ReadOnlySpan<char> rSpan = "-r:".AsSpan();
                ReadOnlySpan<char> referenceSpan = "--reference".AsSpan();
#elif NET5_0_OR_GREATER
                var rSpan = "-r:";
                var referenceSpan = "--reference";
#endif

                if (span.StartsWith(rSpan))
                {
                    References.Add(span.TrimStart(rSpan).ToString());
                }
                else if (span.StartsWith(referenceSpan))
                {
                    References.Add(span.TrimStart(referenceSpan).ToString());
                }
            }
        }

        public IEnumerable<string> GetReferences() => References;

        public string[] ToArray()
        {
            return Commands.ToArray();
        }

        public IEnumerator<string> GetEnumerator()
        {
            IEnumerable<string> enumerable = Commands;
            return enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            IEnumerable enumerable = Commands;
            return enumerable.GetEnumerator();
        }
    }
}
