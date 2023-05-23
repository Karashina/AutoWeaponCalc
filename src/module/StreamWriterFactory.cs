using System.Text;

using CalcsheetGenerator.Interfaces;

namespace CalcsheetGenerator.Module{
    public partial class StreamWriterFactory : IStreamWriterFactory
    {
        public IStreamWriter Create(string Path, bool Append, Encoding type)
        {
            return new _StreamWriter(Path, Append, type);
        }
    }
}