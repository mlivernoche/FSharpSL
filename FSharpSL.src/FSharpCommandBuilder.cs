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

        internal static string[] CommandList(string filePath, IEnumerable<string> references)
        {
            var builder = new FSharpCompilerOptionsBuilder(filePath)
            {
                "--target:library",
                "--highentropyva+",
                "--tailcalls+"
            };

            var list = new HashSet<string>()
            {
                "fsc.exe",
                "-a", filePath,
                "-O",
                "--target:library",
                "--highentropyva+",
                "--tailcalls+",
                "--crossoptimize+",
                "--platform:x64",
                "--noframework",
                "--simpleresolution",
                "-r:" + Path.Combine(Environment.CurrentDirectory, "mscorlib.dll"),
                "-r:" + Path.Combine(Environment.CurrentDirectory, "System.Private.CoreLib.dll"),
                "-r:" + Path.Combine(Environment.CurrentDirectory, "System.Runtime.dll"),
                "-r:" + Path.Combine(Environment.CurrentDirectory, "FSharp.Core.dll"),
                "-r:" + Path.Combine(Environment.CurrentDirectory, "System.Globalization.dll"),
                "-r:" + Path.Combine(Environment.CurrentDirectory, "System.Collections.dll")
            };

            foreach (var reference in references)
            {
                list.Add($"-r:{reference}");
            }

            return list.ToArray();
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
