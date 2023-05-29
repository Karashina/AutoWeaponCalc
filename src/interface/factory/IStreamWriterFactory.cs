using System.Text;

namespace CalcsheetGenerator.Interfaces
{
    public interface IStreamWriterFactory
    {
        public IStreamWriter Create(string Path, bool Append, Encoding type);
    }
}