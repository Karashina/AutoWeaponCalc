namespace Config
{
    public static class Path
    {
        public static class Directiry
        {
        public static readonly string Resource = "../resource/";
        public static readonly string ExecBinary = $"{Resource}execBinary/";
        public static readonly string WeaponData = $"{Resource}weaponData/";
        public static readonly string Input = $"{Resource}input/";
        }

        public static class File
        {
        public static readonly string GcSimWinExe = $"{Directiry.ExecBinary}gcsim.exe";
        public static readonly string ArtifactCsv = $"{Directiry.Input}artifacts.csv";
        public static readonly string SimConfigText = $"{Directiry.Input}config.txt";
        //TODO 実行環境切り替えを実装したときに使用
        public static readonly string GcSimLinuxBin = $"{Directiry.ExecBinary}gcsim";
        public static readonly string GcSimDarwinBin = $"{Directiry.ExecBinary}gcsim";
        }
    }
}