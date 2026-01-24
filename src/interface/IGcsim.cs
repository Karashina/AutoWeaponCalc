namespace CalcsheetGenerator.Interfaces
{
    public interface IGcsim
    {
        public String Exec(string tempSimConfigPath, IProcessFactory? _ProcessFactory = null);
    }
}