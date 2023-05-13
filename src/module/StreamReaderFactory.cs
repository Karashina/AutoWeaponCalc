using CalcsheetGenerator.Interfaces;

namespace CalcsheetGenerator.Module{
    public partial class StreamReaderFactory : IStreamReader
    {
        public StreamReader Create(string Path)
        {
            return new StreamReader(Path);
        }
    }
}