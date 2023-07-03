using System.Text;

namespace CalcsheetGenerator.Interfaces
{
    public interface IStreamWriter : IDisposable
    {
        public void Write(string? Content);
        public void WriteLine(string? Content);
    }
}