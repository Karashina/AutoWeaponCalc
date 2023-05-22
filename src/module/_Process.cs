using CalcsheetGenerator.Interfaces;

namespace CalcsheetGenerator.Module{
    public partial class _Process : IProcess
    {
        public IGcsimProcess Create(string[] args)
        {
            return (IGcsimProcess)new GcsimProcess(args);
        }

    }
}