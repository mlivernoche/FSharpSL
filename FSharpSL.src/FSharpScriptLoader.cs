using System.IO;
using System.Threading.Tasks;

namespace FSharpSL
{
    internal class FSharpScriptLoader
    {
        public virtual byte[] Load(string filePath)
        {
            return File.ReadAllBytes(filePath);
        }

        public virtual async Task<byte[]> LoadAsync(string filePath)
        {
            return await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);
        }
    }
}
