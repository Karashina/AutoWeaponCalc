namespace CalcsheetGenerator.Interfaces
{
    public interface IGcsimProcess
    {
        public void Start();

        public void WaitForExit();

        public string? GetOuptput();
    }
}