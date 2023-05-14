using System.Text;

namespace CalcsheetGenerator.Interfaces
{
    public interface IStreamWriter
    {
        abstract StreamWriter Create(string Path, bool Append, Encoding type);
    }
}