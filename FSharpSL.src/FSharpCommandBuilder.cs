using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSharpSL
{
    internal sealed class FSharpCompilerOptionsBuilder : IEnumerable<string>
    {
        private List<string> Commands { get; } = new List<string>();
        private HashSet<string> Uniques { get; } = new HashSet<string>();
        private HashSet<string> References { get; } = new HashSet<string>();
        public string FileName { get; }

        public FSharpCompilerOptionsBuilder(string fileName)
        {
            FileName = fileName;
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
                if(span.StartsWith("-r:"))
                {
                    References.Add(span.TrimStart("-r:").ToString());
                }
                else if(span.StartsWith("--reference:"))
                {
                    References.Add(span.TrimStart("--reference:").ToString());
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
            return ((IEnumerable<string>)Commands).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<string>)Commands).GetEnumerator();
        }
    }
}
