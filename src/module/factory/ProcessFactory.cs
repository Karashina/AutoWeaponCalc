using CalcsheetGenerator.Interfaces;

namespace CalcsheetGenerator.Module{
    public partial class ProcessFactory : IProcessFactory
    {
        public IGcsimProcess Create(string[] args)
        {
            return (IGcsimProcess) new GcsimProcess(args);
        }

    }
}