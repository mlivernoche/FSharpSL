using System;
using System.Collections.Generic;
using System.Text;

namespace FSharpSL
{
    internal sealed record FSharpScript(FSharpCompilerOptionsBuilder Builder, ReadOnlyMemory<byte> Script);
}
