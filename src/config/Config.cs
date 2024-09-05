namespace CalcsheetGenerator.Config
{
    public static class Path
    {
        public static class Directory
        {
        public static readonly string Resource = "../resource/";
        public static readonly string Out = "../out/";
        public static readonly string Bin = "../bin/";
        public static readonly string ExecBinary = $"{Resource}execBinary/";
        public static readonly string WeaponData = $"{Resource}weaponData/";
        public static readonly string CharData = $"{Resource}chatracterData/";
        public static readonly string Input = $"{Resource}input/";
        }

        public static class File
        {
        public static readonly string GcSimWinExe = $"{Directory.ExecBinary}gcsim.exe";
        public static readonly string ArtifactCsv = $"{Directory.Input}artifacts.csv";
        public static readonly string SimConfigText = $"{Directory.Input}config.txt";
        public static readonly string TempSimConfigText = $"{Directory.Input}temp.txt";
        public static readonly string OutputText = $"{Directory.Bin}Output.txt";
        public static readonly string OptimizedconfigText = $"{Directory.ExecBinary}OptimizedConfig.txt";
        //TODO 実行環境切り替えを実装したときに使用
        public static readonly string GcSimLinuxBin = $"{Directory.ExecBinary}gcsim";
        public static readonly string GcSimDarwinBin = $"{Directory.ExecBinary}gcsim";
        }
    }
}