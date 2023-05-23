using CalcsheetGenerator.Interfaces;

namespace CalcsheetGenerator.Module{
    public partial class StreamReaderFactory : IStreamReaderFactory
    {
        public StreamReader Create(string Path)
        {
            return new StreamReader(Path);
        }
    }
}