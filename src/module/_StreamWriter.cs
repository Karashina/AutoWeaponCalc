using System.Text;

using CalcsheetGenerator.Interfaces;

namespace CalcsheetGenerator.Module{
    public partial class _StreamWriter : IStreamWriter
    {
        public StreamWriter Create(string Path, bool Append, Encoding type)
        {
            return new StreamWriter(Path, Append, type);
        }
    }
}