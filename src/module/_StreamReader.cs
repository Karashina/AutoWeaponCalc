using CalcsheetGenerator.Interfaces;

namespace CalcsheetGenerator.Module{
    public partial class _StreamReader : IStreamReader
    {
        public StreamReader Create(string Path)
        {
            return new StreamReader(Path);
        }
    }
}