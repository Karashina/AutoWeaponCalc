using CalcsheetGenerator.Module;

namespace CalcsheetGenerator.Interfaces
{
    public interface IProcess
    {

        public IGcsimProcess Create(string[] args);
    }
}