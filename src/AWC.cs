﻿using System.Data;
using System.Diagnostics;
using System.Text;

using CalcsheetGenerator.Common;
using CalcsheetGenerator.Enum;
using CalcsheetGenerator.Interfaces;
using CalcsheetGenerator.Module;

namespace CalcsheetGenerator
{
    public class Primary
    {
        private static IPreparation _Preparation = Preparation.GetInstance();
        private static ISettingFileReader _SettingFileReader = SettingFileReader.GetInstance();
        private static ISettingFileWriter _SettingFileWriter = SettingFileWriter.GetInstance();
        private static IFileManager _FileManager = FileManager.GetInstance();
        private static IGcsimManager _GcsimManager = GcsimManager.GetInstance();

        public static void Main()
        {
            try
            {
                //起動時設定
                _Preparation.SelectMode();

                //設定取得
                UserInput InitialSetting = _Preparation.Startup();

                //nullまたは空文字が初期設定のいずれかに入ったていたら強制終了
                if (InitialSetting.IsSetPropertyNullOrEmpty())
                {
                    throw new FormatException(Message.Error.StartupAutomode);
                }

                //最後に出力する表を作成
                DataTable OutputDataTable = new DataTable("Table");

                //カラム名の追加
                OutputDataTable.Columns.Add("武器名");
                OutputDataTable.Columns.Add("精錬R");
                OutputDataTable.Columns.Add("DPS");

                IGcsim _Gcsim = _GcsimManager.CreateGcsimInstance();

                //モードごとに処理
                bool isArtifactModeEnabled = InitialSetting.ArtifactModeSel == "y";
                
                List<ArtifactData> ArtifactList = isArtifactModeEnabled ?
                    _SettingFileReader.GetArtifactList() : // 聖遺物のセットごとの算出
                    new List<ArtifactData>{new ArtifactData(ArtifactPieces._4pc, "", "")}; //武器のみの算出のダミー用聖遺物

                foreach (ArtifactData Artifact in ArtifactList)
                {
                    if (isArtifactModeEnabled) {
                        Console.WriteLine($"{Message.Notice.ProcessStart}{Artifact.Name1} {Artifact.Name2}"); //開始メッセージ
                    }
                    List<WeaponData> WeaponList = _SettingFileReader.GetWeaponList(InitialSetting);
                    
                    foreach (WeaponData Weapon in WeaponList)
                    {
                        string WeaponRefineRank = InitialSetting.WeaponRefineRank;
                        //rarityに応じた自動精錬ランク設定
                        if (InitialSetting.WeaponRefineRank == "0")
                        {
                            WeaponRefineRank = Weapon.Rarity == "1" ? WeaponRefineRank = "1" : WeaponRefineRank = "5"; // 星5なら精錬ランク1 星4なら精錬ランク5に置き換え
                        }

                        string OldTextWeapon = $"{InitialSetting.CharacterName} add weapon=\"<w>\" refine=<r>";
                        string NewTextWeapon = $"{InitialSetting.CharacterName} add weapon=\"{Weapon.NameInternal}\" refine={WeaponRefineRank}";

                        string OldTextArtifact = $"{InitialSetting.CharacterName} add set=\"<a>\" count=<p>;";//置き換え前の文章（聖遺物）
                        string NewTextArtifact = ArtifactPieces._4pc.Equals(Artifact.PiecesCheck) ? //置き換え後の文章（聖遺物）
                            $"{InitialSetting.CharacterName} add set=\"{Artifact.Name1}\" count=4;" : //4セット混合
                            $"{InitialSetting.CharacterName} add set=\"{Artifact.Name1}\" count=2; {Environment.NewLine}{InitialSetting.CharacterName} add set=\"{Artifact.Name2}\" count=2;"; //2セット混合

                        string TextFileContet = _SettingFileReader.GetTextFileContent(Config.Path.File.SimConfigText);
                        string ReplacedContent = Primary.ReplaceText(TextFileContet,OldTextWeapon, NewTextWeapon);
                        if (isArtifactModeEnabled)//聖遺物モード
                        {
                            ReplacedContent = Primary.ReplaceText(ReplacedContent, OldTextArtifact, NewTextArtifact);
                        }
                        _SettingFileWriter.WriteText(Config.Path.File.TempSimConfigText, Append: false, ReplacedContent);

                        Debug.WriteLine("Replaced");

                        float WeaponDps = 0;
                        try
                        {
                            WeaponDps = GetWeaponDps(_Gcsim.Exec(), InitialSetting.CharacterName); //gcsim起動
                        }
                        catch (Exception ge)
                        {
                            // Gcsimでの計算ができなかった場合、処理を継続させるためにエラーを出力
                            // DPSは0とする
                            Console.WriteLine(ge.Message);
                        }

                        Console.WriteLine(Weapon.NameInternal + ":" + WeaponDps); //Consoleに進捗出力

                        OutputDataTable.Rows.Add(Weapon.NameJapanese, WeaponRefineRank, WeaponDps); //tableに結果を格納
                    }
                    string CSVFileName = isArtifactModeEnabled ? $"WeaponDps_{Artifact.Name1}_{Artifact.Name2}.csv" : "WeaponDps.csv";

                    if (Directory.Exists(Config.Path.Directory.Out) == false) //出力ディレクトリがなかったら作る
                    {
                        Directory.CreateDirectory(Config.Path.Directory.Out);
                    }

                    if (File.Exists(Config.Path.Directory.Out + CSVFileName)) //既に同名ファイルがあった場合の処理
                    {
                        Console.WriteLine(Message.Notice.DuplicateCSV);
                        switch (Console.ReadLine() ?? "")
                        {
                            case "y":
                                _FileManager.DeleteFile(Config.Path.Directory.Out + CSVFileName);//yが選択された場合古い方を消す
                                break;
                            case "n":
                                CSVFileName = CSVFileName + "_latest";//nが選択された場合リネームする
                                break;
                            default:
                                throw new FormatException(Message.Error.DuplicateCSVError);
                        }                       
                    }

                    _SettingFileWriter.ExportDataTableToCsv(OutputDataTable, Config.Path.Directory.Out + CSVFileName);

                    OutputDataTable.Clear();//次の聖遺物のため書き出し用リストを初期化
                    if (isArtifactModeEnabled) {
                        Console.WriteLine($"{Message.Notice.ProcessEnd}{Artifact.Name1} {Artifact.Name2}"); //終了メッセージ
                    }
                }
                // 一時ファイルの削除
                _FileManager.DeleteFile(Config.Path.File.TempSimConfigText);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _Environment.Current.Exit(1);
            }
        }

        public static float GetWeaponDps(string GcsimOutput, string CharacterName)
        {
            //DPS数値検索:頭
            string Query1 = $"{CharacterName} total avg dps: ";

            //DPS数値部分のみを切り出す
            string WeaponDps = GcsimOutput.Substring(GcsimOutput.IndexOf(Query1)).Replace(Query1, "");

            //DPS数値検索:足
            //floatに変換して返す
            return float.Parse(WeaponDps.Substring(0, WeaponDps.IndexOf(";")));
        }

        public static string ReplaceText(string Content, string OldText, string NewText)
        {
            string[] TextFileLines = Content.Split(Environment.NewLine);
            List<string> WriteTextFileLines = new List<string>();
            foreach (string Line in TextFileLines)
            {
                WriteTextFileLines.Add(Line.Contains(OldText) ? Line.Replace(OldText, NewText) : Line);
            }
            return string.Join(Environment.NewLine, WriteTextFileLines);
        }
    }
    public class Preparation : IPreparation
    {
        private static Preparation? Instance;

        public Mode _Mode = Mode.None;

        private Preparation()
        {
            //pass
        }
        public static Preparation GetInstance()
        {
            if (Instance == null)
            {
                Preparation.Instance = new Preparation();
            }
            return Preparation.Instance;
        }

        public void SelectMode()
        {
            //モード指定(auto / manual)
            Console.WriteLine(Message.Notice.SelectMode);
            string? UserInputModeSelection = Console.ReadLine();

            switch (UserInputModeSelection)
            {
                case "a":
                    this._Mode = Mode.Auto;
                    break;
                case "m":
                    this._Mode = Mode.Manual;
                    break;
                default:
                    throw new FormatException(Message.Error.SelectMode);
            }
        }

        public UserInput Startup()//manualモード
        {
            if (Mode.None.Equals(this._Mode)){
                throw new Exception(Message.Error.SelectMode);
            }
            string WeaponRefinerank = "0";
            string ArtifactModeSel = "y";

            //キャラ名指定
            Console.WriteLine(Message.Notice.SelectCharctor);
            string CharacterName = Console.ReadLine() ?? "";

            //武器種指定
            Console.WriteLine(Message.Notice.SelectWeapon);
            string WeaponType = Console.ReadLine() ?? "";

            if (Mode.Manual.Equals(this._Mode))
            {
                //精錬ランク指定
                Console.WriteLine(Message.Notice.SelectRefinement);
                WeaponRefinerank = Console.ReadLine() ?? "";

                //聖遺物モード切替
                Console.WriteLine(Message.Notice.SelectArtifactOptimization);
                ArtifactModeSel = Console.ReadLine() ?? "";
            }

            return new UserInput(CharacterName, WeaponType, WeaponRefinerank, ArtifactModeSel);
        }
    }

    //データを格納するレコード
    public record WeaponData(string NameJapanese, string NameInternal, string Rarity);
    public record ArtifactData(string PiecesCheck, string Name1, string Name2);
    public class SettingFileReader : ISettingFileReader
    {
        private static SettingFileReader? Instance;

        private SettingFileReader()
        {
            // pass
        }

        public static SettingFileReader GetInstance()
        {
            if (Instance == null)
            {
                SettingFileReader.Instance = new SettingFileReader();
            }
            return SettingFileReader.Instance;
        }

        public List<WeaponData> GetWeaponList(UserInput InitialSetting, IStreamReaderFactory? _StreamReaderFactory=null) //CSV読み込み（武器）
        {
            //ファイル名
            string CsvPathWeapon = $"{Config.Path.Directory.WeaponData}{InitialSetting.WeaponType}.csv";

            //取得したデータを保存するリスト
            List<WeaponData> WeaponList = new List<WeaponData>();

            //CSV読み込み部分
            using (StreamReader WeaponCsvReader = (_StreamReaderFactory ?? new StreamReaderFactory()).Create(CsvPathWeapon))
            {
                while (0 <= WeaponCsvReader.Peek())
                {
                    //カンマ区切りで分割して配列で格納する
                    string[]? Column = WeaponCsvReader.ReadLine()?.Split(',');
                    if (Column is null) continue;

                    //リストにデータを追加する
                    WeaponList.Add(new WeaponData(Column[0], Column[1], Column[2]));
                }
            }

            //先頭行は項目名なのでスキップする(CSVのヘッダ)
            if (0 < WeaponList.Count())
            {
                WeaponList.RemoveAt(0); 
            }
            return WeaponList;
        }

        public List<ArtifactData> GetArtifactList(IStreamReaderFactory? _StreamReaderFactory=null)//CSV読み込みと計算
        {
            //取得したデータを保存するリスト
            List<ArtifactData> ArtifactList = new List<ArtifactData>();

            //ファイルを開く
            using (StreamReader ArtifactCsvReader = (_StreamReaderFactory?? new StreamReaderFactory()).Create(Config.Path.File.ArtifactCsv))
            {
                while (0 <= ArtifactCsvReader.Peek())
                {
                    //カンマ区切りで分割して配列で格納する
                    string[]? Column = ArtifactCsvReader.ReadLine()?.Split(',');
                    if (Column is null) continue;

                    //聖遺物2スロット目を使わない場合出力時に"〇〇4pc"となるようにする
                    if (Column[0] == "1")
                    {
                        Column[2] = "4pc";
                    }

                    //tableにデータを追加する
                    ArtifactList.Add(new ArtifactData(Column[0], Column[1], Column[2]));
                }
            }

            //先頭行は項目名なのでスキップする(CSVのヘッダ)
            if (0 < ArtifactList.Count())
            {
                ArtifactList.RemoveAt(0); 
            }

            return ArtifactList;
        }

        public string GetTextFileContent(string TextFilePath, IStreamReaderFactory? _StreamReaderFactory=null)
        {
            using (StreamReader TextReader = (_StreamReaderFactory ?? new StreamReaderFactory()).Create(TextFilePath))
            {
                return TextReader.ReadToEnd();
            }
        }
    }
    public class SettingFileWriter : ISettingFileWriter
    {
        private static SettingFileWriter? Instance;

        private SettingFileWriter()
        {
            //pass
        }

        public static SettingFileWriter GetInstance()
        {
            if (Instance == null)
            {
                SettingFileWriter.Instance = new SettingFileWriter();
            }
            return SettingFileWriter.Instance;
        }

        public void WriteText(string FilePath, bool Append, string TextContent, IStreamWriterFactory? StreamWriterFactory=null)
        {
            //BOM無しのUTF8でテキストファイルを作成する
            Encoding UTF8WithoutBOM = new UTF8Encoding(false);

            using (IStreamWriter TextWriter = (StreamWriterFactory ?? new StreamWriterFactory()).Create(FilePath, Append, UTF8WithoutBOM))
            {
                TextWriter.Write(TextContent);
            }
        }

        public void ExportDataTableToCsv(DataTable OutputDataTable, string CsvFileName, IStreamWriterFactory? StreamWriterFactory=null)
        {
            using (IStreamWriter CsvWriter = (StreamWriterFactory ?? new StreamWriterFactory()).Create(CsvFileName, true, Encoding.UTF8))
            {
                //ヘッダーを出力
                CsvWriter.WriteLine(string.Join(
                    ",",
                    OutputDataTable
                        .Columns
                        .Cast<DataColumn>()
                        .Select(c => c.Caption)
                        .Select(field => field.ToString())
                ));
                //内容を出力
                foreach (DataRow CsvRow in OutputDataTable.Rows)
                {
                    CsvWriter.WriteLine(string.Join(
                    ",",
                    CsvRow
                        .ItemArray
                        .Select(i => i?.ToString())
                        .Select(field => field?.ToString())
                    ));
                }
            }
        }
    }
    public class Gcsim : IGcsim
    {
        public String Exec(IProcessFactory? _ProcessFactory=null) //gcsimで計算
        {
            // Processクラスのオブジェクトを作成
            IGcsimProcess SubstatOptimizationProcess =  (_ProcessFactory ?? new ProcessFactory()).Create(
            new[] {
                Config.Path.File.GcSimWinExe, // 1回目にgcsimに渡す引数
                $"-c={Config.Path.File.TempSimConfigText}",
                "-substatOptim=true",
                "-out=OptimizedConfig.txt"
            });

            // プロセス起動1回目
            SubstatOptimizationProcess.Start();
            Console.WriteLine(Message.Notice.SubstatOptimizationStart);

            // 標準出力を取得
            string? SubstatOptimizationProcessOutput = SubstatOptimizationProcess.GetOuptput();

            // 標準出力を表示
            Debug.WriteLine(SubstatOptimizationProcessOutput);
            SubstatOptimizationProcess.WaitForExit();
            Console.WriteLine(Message.Notice.SubstatOptimizationEnd);

            
            // Processクラスのオブジェクトを作成
            IGcsimProcess CalcDPSProcess =  (_ProcessFactory ?? new ProcessFactory()).Create(
            new[] {
                Config.Path.File.GcSimWinExe, // 2回目にgcsimに渡す引数
                "-c=OptimizedConfig.txt"
            });

            // プロセス起動2回目
            CalcDPSProcess.Start();

            // 標準出力を取得
            string? CalcDPSProcessOutput = CalcDPSProcess.GetOuptput();
            CalcDPSProcess.WaitForExit();

            // 標準出力を表示
            Debug.WriteLine(CalcDPSProcessOutput);

            //エラー分岐
            if (string.IsNullOrEmpty(CalcDPSProcessOutput))
            {
                // 出力がない場合は、エラーとする
                throw new Exception(Message.Error.GcsimOutputNone);
            }
            return CalcDPSProcessOutput;
        }
    }
}