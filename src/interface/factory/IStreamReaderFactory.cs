namespace CalcsheetGenerator.Interfaces
{
    public interface IStreamReaderFactory
    {
        abstract StreamReader Create(string Path);
    }
}