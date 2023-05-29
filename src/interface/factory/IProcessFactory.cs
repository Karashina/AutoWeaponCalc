namespace CalcsheetGenerator.Interfaces
{
    public interface IProcessFactory
    {

        public IGcsimProcess Create(string[] args);
    }
}