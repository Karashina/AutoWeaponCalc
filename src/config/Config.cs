using System;

namespace CalcsheetGenerator.Config
{
    public static class Path
    {
        // Define BasePath as the directory containing the executable.
        // For standard .NET apps, this is AppDomain.CurrentDomain.BaseDirectory.
        // For SingleFile apps, this usually points to the temp extraction folder if not Configured correctly,
        // but since we don't extract (StartInfo.UseShellExecute = false typically runs validly), let's ensure we use the location of the module.
        // However, a safe bet for a portable "bin/resource" structure is to anchor on BaseDirectory.
        public static readonly string BasePath = AppDomain.CurrentDomain.BaseDirectory;

        public static class Directory
        {
            // Use Path.Combine to ensure correct separator usage
            public static readonly string Resource = System.IO.Path.Combine(BasePath, "../resource/");
            public static readonly string Out = System.IO.Path.Combine(BasePath, "../out/");
            public static readonly string Bin = System.IO.Path.Combine(BasePath, "../bin/");
            
            public static readonly string ExecBinary = System.IO.Path.Combine(Resource, "execBinary/");
            public static readonly string WeaponData = System.IO.Path.Combine(Resource, "weaponData/");
            public static readonly string CharData = System.IO.Path.Combine(Resource, "characterData/");
            public static readonly string Input = System.IO.Path.Combine(Resource, "input/");
        }

        public static class File
        {
            public static readonly string GcSimWinExe = System.IO.Path.Combine(Directory.ExecBinary, "gcsim.exe");
            public static readonly string ArtifactCsv = System.IO.Path.Combine(Directory.Input, "artifacts.csv");
            public static readonly string SimConfigText = System.IO.Path.Combine(Directory.Input, "config.txt");
            public static readonly string TempSimConfigText = System.IO.Path.Combine(Directory.Input, "temp.txt");
            public static readonly string OutputText = System.IO.Path.Combine(Directory.Bin, "Output.txt");
            public static readonly string OptimizedconfigText = System.IO.Path.Combine(Directory.ExecBinary, "OptimizedConfig.txt");
            //TODO 実行環境切り替えを実装したときに使用
            public static readonly string GcSimLinuxBin = System.IO.Path.Combine(Directory.ExecBinary, "gcsim");
            public static readonly string GcSimDarwinBin = System.IO.Path.Combine(Directory.ExecBinary, "gcsim");
        }
    }
}