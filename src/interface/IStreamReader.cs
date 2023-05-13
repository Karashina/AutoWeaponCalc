namespace CalcsheetGenerator.Interfaces
{
    public interface IStreamReader
    {
        abstract StreamReader Create(string Path);
    }
}