using System.Text;

using CalcsheetGenerator.Interfaces;

namespace CalcsheetGenerator.Module{
    public partial class _StreamWriter : IStreamWriter
    {
        StreamWriter? instance;

        public _StreamWriter(string Path, bool Append, Encoding type)
        {
            this.instance = new StreamWriter(Path, Append, type);
        }
        public void Write(string? Content)
        {
            instance?.Write(Content);
        }
        public void WriteLine(string? Content)
        {
            instance?.WriteLine(Content);
        }

        public void Dispose()
        {
            instance?.Dispose();
        }
    }
}