using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSharpSE
{
    public sealed class FSharpCompilerOptionsBuilder : IEnumerable<string>
    {
        private List<string> Commands { get; } = new List<string>();
        private HashSet<string> Uniques { get; } = new HashSet<string>();
        public string FileName { get; }

        public FSharpCompilerOptionsBuilder(string fileName)
        {
            FileName = fileName;
            Add("fsc.exe");
            Add("-a");
            Add(fileName);
            Add("-O");
            Add("--target:library");
        }

        public void Add(string command)
        {
            if(Uniques.Add(command))
            {
                Commands.Add(command);
            }
        }

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
