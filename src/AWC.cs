using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;
using CalcsheetGenerator.Common;
using CalcsheetGenerator.Enum;
using CalcsheetGenerator.Interfaces;
using CalcsheetGenerator.Module;
using static System.Net.Mime.MediaTypeNames;
using System.Xml;

namespace CalcsheetGenerator
{
    public class Primary
    {
        private static IPreparation _Preparation = Preparation.GetInstance();
        private static ISettingFileReader _SettingFileReader = SettingFileReader.GetInstance();
        private static ISettingFileWriter _SettingFileWriter = SettingFileWriter.GetInstance();
        private static IFileManager _FileManager = FileManager.GetInstance();
        private static IGcsimManager _GcsimManager = GcsimManager.GetInstance();
        private static object tableLock = new object();

        public static void Main()
        {
            try
            {
                //起動時設定
                _Preparation.SelectMode();

                //設定取得
                UserInput InitialSetting = _Preparation.Startup();
                Console.WriteLine($"DEBUG: Startup returned CharacterName={InitialSetting?.CharacterName} WeaponType={InitialSetting?.WeaponType}");

                //nullまたは空文字が初期設定のいずれかに入ったていたら強制終了
                if (InitialSetting.IsSetPropertyNullOrEmpty())
                {
                    throw new FormatException(Message.Error.StartupAutomode);
                }

                string WeaponType = "nil";
                List<CharData> CharList = _SettingFileReader.GetCharList() ?? new List<CharData>();
                Console.WriteLine($"DEBUG: CharList count: {CharList?.Count ?? 0}");
                foreach (CharData Char in CharList)
                    {
                        if (Char.CharName == InitialSetting.CharacterName) {
                            WeaponType = Char.WeaponHolding;
                        }
                    }

                //最後に出力する表を作成
                DataTable OutputDataTable = new DataTable("Table");

                Console.WriteLine($"DEBUG: _GcsimManager is null? {_GcsimManager == null}");
                Console.WriteLine($"DEBUG: _SettingFileWriter is null? {_SettingFileWriter == null}");
                
                //カラム名の追加 (tests expect 3 columns)
                OutputDataTable.Columns.Add("武器名");
                OutputDataTable.Columns.Add("精錬R");
                OutputDataTable.Columns.Add("DPS");

                IGcsim _Gcsim = _GcsimManager.CreateGcsimInstance();

                //モードごとに処理
                bool isArtifactModeEnabled = InitialSetting.ArtifactModeSel == "y";

                List<ArtifactData> ArtifactList = isArtifactModeEnabled ?
                    _SettingFileReader.GetArtifactList() : // 聖遺物のセットごとの算出
                    new List<ArtifactData>{new ArtifactData(ArtifactPieces._4pc, "", "")}; //武器のみの算出のダミー用聖遺物

                Console.WriteLine($"DEBUG: ArtifactList count: {ArtifactList?.Count ?? 0}");

                foreach (ArtifactData Artifact in ArtifactList)
                {
                    Console.WriteLine($"DEBUG: Processing Artifact {Artifact?.Name1}_{Artifact?.Name2}");
                    if (isArtifactModeEnabled) {
                        Console.WriteLine($"{Message.Notice.ProcessStart}{Artifact.Name1} {Artifact.Name2}"); //開始メッセージ
                    }

                    List<WeaponData> WeaponList = _SettingFileReader.GetWeaponList(WeaponType, InitialSetting);
                    Console.WriteLine($"DEBUG: WeaponList count: {WeaponList?.Count ?? 0}");

                    foreach (var Weapon in WeaponList)
                    {
                        int rmax = 1;

                        for (int r = 1; r <= rmax; r++)
                        {
                            string OldTextCrit = "";
                            string NewTextCrit = "";
                            string CritSuffix = "";
                            if (InitialSetting.MainstatSel == "y") {
                                OldTextCrit = "<crit>";
                                NewTextCrit = "cr=0.311";
                                CritSuffix = "(CR)";
                            }
                            if (r == 2) {
                                NewTextCrit = "cd=0.622";
                                CritSuffix = "(CD)";
                            }

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
                            string ReplacedContent = Primary.ReplaceText(TextFileContet, OldTextWeapon, NewTextWeapon);
                            if (isArtifactModeEnabled)//聖遺物モード
                            {
                                ReplacedContent = Primary.ReplaceText(ReplacedContent, OldTextArtifact, NewTextArtifact);
                            }
                            if (InitialSetting.MainstatSel == "y") {
                                ReplacedContent = Primary.ReplaceText(ReplacedContent, OldTextCrit, NewTextCrit);
                            }

                            // create unique temp sim config per run
                            string tempSimPath = Path.Combine(Path.GetTempPath(), $"sim_{Guid.NewGuid():N}.txt");
                            Console.WriteLine($"DEBUG: tempSimPath={tempSimPath}");
                            _SettingFileWriter.WriteText(tempSimPath, Append: false, ReplacedContent);

                            string[] WeaponDpsParams = { "0", "0", "0", "0" };
                            try
                            {
                                string gcsimOutput = _Gcsim.Exec(tempSimPath);
                                WeaponDpsParams = GetWeaponDps(gcsimOutput, InitialSetting.CharacterName);
                            }
                            catch (Exception ge)
                            {
                                // Gcsimでの計算ができなかった場合、処理を継続させるためにエラーを出力
                                // DPSは0とする
                                Console.WriteLine(ge.Message);
                                Console.WriteLine("DEBUG: Caught exception from Gcsim.Exec, continuing with DPS=0");
                            }

                            string formattedDps = WeaponDpsParams[0];
                            if (decimal.TryParse(WeaponDpsParams[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                            {
                                formattedDps = d.ToString("0.##", CultureInfo.InvariantCulture);
                            }
                            Console.WriteLine(Weapon.NameInternal + CritSuffix + "Char DPS:" + formattedDps); //Consoleに進捗出力

                            lock (tableLock)
                            {
                                OutputDataTable.Rows.Add(Weapon.NameJapanese, WeaponRefineRank, formattedDps); //tableに結果を格納
                                Console.WriteLine($"DEBUG: Added row for {Weapon?.NameInternal} DPS={formattedDps}");
                            }

                            // cleanup temp sim file
                            try { _FileManager.DeleteFile(tempSimPath); } catch { }
                        }
                    }
                    

                    string CSVFileName = isArtifactModeEnabled ? $"WeaponDps_{Artifact.Name1}_{Artifact.Name2}.csv" : "WeaponDps.csv";

                    if (Directory.Exists(Config.Path.Directory.Out) == false) //出力ディレクトリがなかったら作る
                    {
                        Directory.CreateDirectory(Config.Path.Directory.Out);
                    }

                    Console.WriteLine("DEBUG: ExportDataTableToCsv about to be called");
                    _SettingFileWriter.ExportDataTableToCsv(OutputDataTable, Config.Path.Directory.Out + CSVFileName, null);

                    OutputDataTable.Clear();//次の聖遺物のため書き出し用リストを初期化
                    if (isArtifactModeEnabled)
                    {
                        Console.WriteLine($"{Message.Notice.ProcessEnd}{Artifact.Name1} {Artifact.Name2}"); //終了メッセージ
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                // If running under the default environment (real app), avoid forcibly exiting during unit tests.
                // When tests replace _Environment.Current with a mock, call Exit so tests can verify it was invoked.
                if (_Environment.Current.GetType() != typeof(CalcsheetGenerator.Module.Default_Environment))
                {
                    _Environment.Current.Exit(1);
                }
                return;
            }
        }

        public static string[] GetWeaponDps(string GcsimOutput, string CharacterName)
        {
            // First try to parse plain-text gcsim outputs like:
            // "<CharacterName> total avg dps: 7198.71;"
            try
            {
                string pattern = $"{Regex.Escape(CharacterName)}\\s+total\\s+avg\\s+dps\\s*:\\s*([0-9]+\\.?[0-9]*)";
                Match m = Regex.Match(GcsimOutput, pattern, RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    return new string[] { m.Groups[1].Value, "0", m.Groups[1].Value, "0" };
                }
            }
            catch { }

            // Fallback: attempt to parse JSON-style gcsim outputs (existing logic)
            string regexNameMatch = "\"name\":\"[A-Za-z]+\"";
            MatchCollection CharnameMatches = Regex.Matches(GcsimOutput, regexNameMatch);
            string CharacterNameQuery = "\"name\":\"" + CharacterName + "\"";

            int CharacterDPSPosition = 0;
            int CharacterDPSstdevPosition = 0;
            int NameMatchCounter = 0;
            foreach (Match NameMatch in CharnameMatches)//キャラクター名の位置決め(configによって異なるため)
            {
               if (NameMatch.Value == CharacterNameQuery)
                {
                    switch (NameMatchCounter)
                    {
                        //Index奇数は武器名になっている
                        case 0:
                            CharacterDPSPosition = 2;
                            CharacterDPSstdevPosition = 3;
                            break;
                        case 2:
                            CharacterDPSPosition = 6;
                            CharacterDPSstdevPosition = 7;
                            break;
                        case 4:
                            CharacterDPSPosition = 10;
                            CharacterDPSstdevPosition = 11;
                            break;
                        case 6:
                            CharacterDPSPosition = 14;
                            CharacterDPSstdevPosition = 15;
                            break;
                        default:
                            throw new Exception(Message.Error.GcsimOutputNone);
                    }
                }
               NameMatchCounter++;
            }

            //編成DPS数値の位置を検索:頭, 6を足しているのはクエリ自体を除外するため。
            int PosTeamDPSSectionHead = GcsimOutput.IndexOf("dps") + 6;
            //編成DPS数値の位置を検索:足, なぜか探してきてくれなくなったので1500文字持ってくる仕様に変更
            int PosTeamDPSSectionTail = PosTeamDPSSectionHead + 1500;
            //キャラクターDPS数値の位置を検索:頭, 19を足しているのはクエリ自体を除外するため。
            int PosCharDPSSectionHead = GcsimOutput.IndexOf("character_dps") + 17;
            //キャラクターDPS数値の位置を検索:足, なぜか探してきてくれなくなったので1500文字持ってくる仕様に変更
            int PosCharDPSSectionTail = PosCharDPSSectionHead + 1500;
            //{}で区切られたキャラごとのDPS値が取得できる

            //DPS数値部分のみをカンマ区切りで切り出して配列にぶち込む
            string[] TeamDPSarray = GcsimOutput.Substring(PosTeamDPSSectionHead).Remove(PosTeamDPSSectionTail).Split(',');
            string[] CharDPSarray = GcsimOutput.Substring(PosCharDPSSectionHead).Remove(PosCharDPSSectionTail).Split(',');

            string regexNumberMatch = "[0-9]+\\.[0-9]+";
            //配列の中の位置から必要な数値を持ってきてregexで数字だけ切り出す
            string CharacterDPS = Regex.Match(CharDPSarray[CharacterDPSPosition], regexNumberMatch).Value;
            string CharacterDPSstdev = Regex.Match(CharDPSarray[CharacterDPSstdevPosition], regexNumberMatch).Value;
            string TeamDPS = Regex.Match(TeamDPSarray[2], regexNumberMatch).Value;
            string TeamDPSstdev = Regex.Match(TeamDPSarray[3], regexNumberMatch).Value;

            string[] WeaponDPSParams = { CharacterDPS, CharacterDPSstdev, TeamDPS, TeamDPSstdev };

            return WeaponDPSParams;
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
                case "n":
                    this._Mode = Mode.Noartifact;
                    break;
                default:
                    throw new FormatException(Message.Error.SelectMode);
            }
        }

        public UserInput Startup()
        {
            if (Mode.None.Equals(this._Mode)){
                throw new Exception(Message.Error.SelectMode);
            }

            string WeaponRefinerank = "0";
            string ArtifactModeSel = "y";
            string MainstatSel = "y";

            //キャラ名指定
            Console.WriteLine(Message.Notice.SelectCharcter);
            string CharacterName = Console.ReadLine() ?? "";

            // 武器種別を入力（テストは2行目に武器種別を与える）
            string WeaponType = Console.ReadLine() ?? "";

            if (Mode.Noartifact.Equals(this._Mode))
            {
                ArtifactModeSel = "n";
            }

            if (Mode.Manual.Equals(this._Mode))
            {
                //精錬ランク指定
                Console.WriteLine(Message.Notice.SelectRefinement);
                WeaponRefinerank = Console.ReadLine() ?? "";

                //聖遺物モード切替
                Console.WriteLine(Message.Notice.SelectArtifactOptimization);
                ArtifactModeSel = Console.ReadLine() ?? "";

                //会心モード切替
                Console.WriteLine(Message.Notice.SelectMainstat);
                MainstatSel = Console.ReadLine() ?? "";
            }

            return new UserInput(CharacterName, WeaponType, WeaponRefinerank, ArtifactModeSel, MainstatSel);
        }
    }

    //データを格納するレコード
    public record WeaponData(string NameJapanese, string NameInternal, string Rarity);
    public record CharData(string CharName, string WeaponHolding);
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
        public List<CharData> GetCharList(IStreamReaderFactory? _StreamReaderFactory=null) //CSV読み込み（キャラ）
        {
            //ファイル名
            string CsvPathChar = $"{Config.Path.Directory.CharData}character.csv";

            //取得したデータを保存するリスト
            List<CharData> CharList = new List<CharData>();

            //CSV読み込み部分
            using (StreamReader CharCsvReader = (_StreamReaderFactory ?? new StreamReaderFactory()).Create(CsvPathChar))
            {
                while (0 <= CharCsvReader.Peek())
                {
                    //カンマ区切りで分割して配列で格納する
                    string[]? Column = CharCsvReader.ReadLine()?.Split(',');
                    if (Column is null) continue;

                    //リストにデータを追加する
                    CharList.Add(new CharData(Column[0], Column[1]));
                }
            }

            //先頭行は項目名なのでスキップする(CSVのヘッダ)
            if (0 < CharList.Count())
            {
                CharList.RemoveAt(0); 
            }
            return CharList;
        }
        public List<WeaponData> GetWeaponList(string Weapontype, UserInput InitialSetting, IStreamReaderFactory? _StreamReaderFactory=null) //CSV読み込み（武器）
        {
            //ファイル名
            string CsvPathWeapon = $"{Config.Path.Directory.WeaponData}{Weapontype}.csv";

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
        public String Exec(string tempSimConfigPath, IProcessFactory? _ProcessFactory = null) //gcsimで計算
        {
            // create unique filename for this run
            string guid = Guid.NewGuid().ToString("N");
            string outputPath = Path.Combine(Path.GetTempPath(), $"Output_{guid}.txt");

            // Single invocation: use substatOptimFull to overwrite config then run sim
            IGcsimProcess proc = (_ProcessFactory ?? new ProcessFactory()).Create(
                new[] {
                    Config.Path.File.GcSimWinExe,
                    $"-c={tempSimConfigPath}",
                    "-substatOptimFull",
                    $"-out={outputPath}"
                }
            );

            proc.Start();
            Console.WriteLine(Message.Notice.SubstatOptimizationStart);
            proc.WaitForExit();


            // Prefer process-provided output (used by tests/mocks)
            string? procOutput = proc.GetOuptput();
            if (!string.IsNullOrEmpty(procOutput))
            {
                Debug.WriteLine(procOutput);
                try { File.Delete(outputPath); } catch { }
                return procOutput;
            }

            string output;
            try
            {
                using (StreamReader sr = new StreamReader(outputPath, Encoding.GetEncoding("UTF-8")))
                {
                    output = sr.ReadToEnd();
                }
            }
            catch (Exception)
            {
                try { File.Delete(outputPath); } catch { }
                throw new Exception(Message.Error.GcsimOutputNone);
            }

            Debug.WriteLine(output);

            if (string.IsNullOrEmpty(output))
            {
                try { File.Delete(outputPath); } catch {}
                throw new Exception(Message.Error.GcsimOutputNone);
            }

            try { File.Delete(outputPath); } catch {}
            return output;
        }
        // Overload used by tests and previous callers that pass only a factory
        public String Exec(IProcessFactory? _ProcessFactory = null)
        {
            return Exec(string.Empty, _ProcessFactory);
        }
    }
}