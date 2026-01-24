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
    /// <summary>
    /// Main entry point class for the application.
    /// Manages the calculation flow, gcsim execution, and data export.
    /// </summary>
    public class Primary
    {
        private static IPreparation _Preparation = Preparation.GetInstance();
        private static ISettingFileReader _SettingFileReader = SettingFileReader.GetInstance();
        private static ISettingFileWriter _SettingFileWriter = SettingFileWriter.GetInstance();
        private static IFileManager _FileManager = FileManager.GetInstance();
        private static IGcsimManager _GcsimManager = GcsimManager.GetInstance();
        private static readonly object tableLock = new object();

        // Optimized Regex patterns
        private static readonly Regex PlainTextDpsRegex = new Regex(@"total\s+avg\s+dps\s*:\s*([0-9]+\.?[0-9]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        // reverted to basic pattern to ensure compatibility with older logic
        private static readonly Regex JsonNameRegex = new Regex("\"name\":\"[A-Za-z]+\"", RegexOptions.Compiled);
        private static readonly Regex NumberRegex = new Regex("[0-9]+\\.[0-9]+", RegexOptions.Compiled);

        public static void Main()
        {
            try
            {
                // Startup configuration
                _Preparation.SelectMode();

                // Get user settings
                UserInput? searchSettings = _Preparation.Startup();
                Console.WriteLine($"DEBUG: Startup returned CharacterName={searchSettings?.CharacterName} WeaponType={searchSettings?.WeaponType}");

                // Validate settings
                if (searchSettings == null || searchSettings.IsSetPropertyNullOrEmpty())
                {
                    throw new FormatException(Message.Error.StartupAutomode);
                }

                // Determine weapon type if not explicitly provided or for validation
                string weaponType = "nil";
                List<CharData> charList = _SettingFileReader.GetCharList() ?? new List<CharData>();
                Console.WriteLine($"DEBUG: CharList count: {charList.Count}");
                
                var targetChar = charList.FirstOrDefault(c => c.CharName == searchSettings.CharacterName);
                if (targetChar != null)
                {
                    weaponType = targetChar.WeaponHolding;
                }

                // Prepare output table
                DataTable outputDataTable = new DataTable("Table");
                Console.WriteLine($"DEBUG: _GcsimManager is null? {_GcsimManager == null}");
                Console.WriteLine($"DEBUG: _SettingFileWriter is null? {_SettingFileWriter == null}");
                
                // Add columns (Weapon Name, Refinement, DPS)
                outputDataTable.Columns.Add("武器名");
                outputDataTable.Columns.Add("精錬R");
                outputDataTable.Columns.Add("DPS");

                if (_GcsimManager == null) throw new InvalidOperationException("_GcsimManager is not initialized.");
                IGcsim gcsimInstance = _GcsimManager.CreateGcsimInstance();

                // Check artifacts mode
                bool isArtifactModeEnabled = searchSettings.ArtifactModeSel == "y";

                List<ArtifactData> artifactList = isArtifactModeEnabled ?
                    _SettingFileReader.GetArtifactList() : // Calculate for each artifact set
                    new List<ArtifactData>{new ArtifactData(ArtifactPieces._4pc, "", "")}; // Dummy artifact if disabled

                if (artifactList == null) artifactList = new List<ArtifactData>(); 
                Console.WriteLine($"DEBUG: ArtifactList count: {artifactList.Count}");

                // Main calculation loop
                foreach (ArtifactData artifact in artifactList)
                {
                    if (artifact == null) continue;
                    ProcessArtifact(artifact, searchSettings, weaponType, isArtifactModeEnabled, outputDataTable, gcsimInstance);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                // If running under the default environment (real app), avoid forcibly exiting during unit tests.
                if (_Environment.Current.GetType() != typeof(CalcsheetGenerator.Module.Default_Environment))
                {
                    _Environment.Current.Exit(1);
                }
                return;
            }
        }

        /// <summary>
        /// Process a single artifact set configuration.
        /// </summary>
        private static void ProcessArtifact(ArtifactData artifact, UserInput settings, string weaponType, bool isArtifactModeEnabled, DataTable outputTable, IGcsim gcsim)
        {
            if (artifact == null || settings == null) return;
            
            Console.WriteLine($"DEBUG: Processing Artifact {artifact.Name1}_{artifact.Name2}");
            if (isArtifactModeEnabled) {
                Console.WriteLine($"{Message.Notice.ProcessStart}{artifact.Name1} {artifact.Name2}");
            }

            List<WeaponData> weaponList = _SettingFileReader.GetWeaponList(weaponType, settings);
            if (weaponList == null) weaponList = new List<WeaponData>();
            
            Console.WriteLine($"DEBUG: WeaponList count: {weaponList.Count}");

            string baseTemplateContent = _SettingFileReader.GetTextFileContent(Config.Path.File.SimConfigText);

            // Validate config template before processing weapons
            string validationTargetWeapon = $"{settings.CharacterName} add weapon=\"<w>\" refine=<r>";
            if (!baseTemplateContent.Contains(validationTargetWeapon))
            {
                 // Check for mismatch
                var mismatchMatch = System.Text.RegularExpressions.Regex.Match(baseTemplateContent, @"^\s*([a-zA-Z0-9_]+)\s+add\s+weapon=""<w>""", System.Text.RegularExpressions.RegexOptions.Multiline);
                if (mismatchMatch.Success)
                {
                    string foundName = mismatchMatch.Groups[1].Value;
                    if (!string.Equals(foundName, settings.CharacterName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"CONFIG ERROR: Template contains placeholder for character '{foundName}' (line: '{mismatchMatch.Value.Trim()}'), but you selected '{settings.CharacterName}'. Please update 'resource/input/config.txt' to use '{settings.CharacterName}' or selects '{foundName}'.");
                    }
                }

                if (baseTemplateContent.Contains("<w>"))
                {
                        throw new InvalidOperationException($"CONFIG ERROR: Found '<w>' placeholder but the line did not match exact pattern '{validationTargetWeapon}'. Check for extra spaces or syntax in 'resource/input/config.txt'.");
                }
                else
                {
                        throw new InvalidOperationException($"CONFIG ERROR: Could not find weapon placeholder '<w>' in 'resource/input/config.txt'. The tool requires '... add weapon=\"<w>\" ...' to function.");
                }
            }

            if (isArtifactModeEnabled)
            {
                string validationTargetArtifact = $"{settings.CharacterName} add set=\"<a>\" count=<p>;";
                if (!baseTemplateContent.Contains(validationTargetArtifact))
                {
                    throw new InvalidOperationException($"CONFIG ERROR: Artifact Mode is enabled but could not find placeholder '{validationTargetArtifact}' in 'resource/input/config.txt'. Check character name match or spacing.");
                }
            }

            foreach (var weapon in weaponList)
            {
                if (weapon == null) continue;
                // Currently only runs once (rmax = 1)
                // If support for multiple refinements is needed, change rmax or loop
                int rmax = 1; 

                for (int r = 1; r <= rmax; r++)
                {
                    ProcessWeaponRefinement(r, weapon, artifact, settings, isArtifactModeEnabled, baseTemplateContent, outputTable, gcsim);
                }
            }

            // Export to CSV
            string csvFileName = isArtifactModeEnabled ? $"WeaponDps_{artifact.Name1}_{artifact.Name2}.csv" : "WeaponDps.csv";

            if (!Directory.Exists(Config.Path.Directory.Out))
            {
                Directory.CreateDirectory(Config.Path.Directory.Out);
            }

            Console.WriteLine("DEBUG: ExportDataTableToCsv about to be called");
            _SettingFileWriter.ExportDataTableToCsv(outputTable, Config.Path.Directory.Out + csvFileName, null);

            outputTable.Clear(); // Clear table for next artifact set
            
            if (isArtifactModeEnabled)
            {
                Console.WriteLine($"{Message.Notice.ProcessEnd}{artifact.Name1} {artifact.Name2}");
            }
        }

        private static void ProcessWeaponRefinement(int refinement, WeaponData weapon, ArtifactData artifact, UserInput settings, bool isArtifactMode, string templateContent, DataTable table, IGcsim gcsim)
        {
            try
            {
                string oldTextCrit = "";
                string newTextCrit = "";
                string critSuffix = "";
                
                if (settings.MainstatSel == "y") {
                    oldTextCrit = "<crit>";
                    newTextCrit = "cr=0.311";
                    critSuffix = "(CR)";
                }
                if (refinement == 2) { // Logic from original code, though rmax is 1
                    newTextCrit = "cd=0.622";
                    critSuffix = "(CD)";
                }

                string weaponRefineRank = settings.WeaponRefineRank;
                // Auto-set refinement rank based on rarity if set to "0"
                if (settings.WeaponRefineRank == "0")
                {
                    weaponRefineRank = weapon.Rarity == "1" ? "1" : "5"; // R1 for 5*, R5 for 4*
                }

                string oldTextWeapon = $"{settings.CharacterName} add weapon=\"<w>\" refine=<r>";
                

                string newTextWeapon = $"{settings.CharacterName} add weapon=\"{weapon.NameInternal}\" refine={weaponRefineRank}";

                string oldTextArtifact = $"{settings.CharacterName} add set=\"<a>\" count=<p>;";
                string newTextArtifact = ArtifactPieces._4pc.Equals(artifact.PiecesCheck) ?
                    $"{settings.CharacterName} add set=\"{artifact.Name1}\" count=4;" :
                    $"{settings.CharacterName} add set=\"{artifact.Name1}\" count=2; {Environment.NewLine}{settings.CharacterName} add set=\"{artifact.Name2}\" count=2;";

                // Replace content in template
                string replacedContent = templateContent.Replace(oldTextWeapon, newTextWeapon);
                
                if (isArtifactMode)
                {
                    replacedContent = replacedContent.Replace(oldTextArtifact, newTextArtifact);
                }
                if (settings.MainstatSel == "y") {
                    replacedContent = replacedContent.Replace(oldTextCrit, newTextCrit);
                }

                // Create unique temp sim config
                string tempSimPath = Path.Combine(Path.GetTempPath(), $"sim_{Guid.NewGuid():N}.txt");
                Console.WriteLine($"DEBUG: tempSimPath={tempSimPath}");
                
                _SettingFileWriter.WriteText(tempSimPath, Append: false, replacedContent);

                string[] weaponDpsParams = { "0", "0", "0", "0" };
                string gcsimOutput = "";
                
                try
                {
                    gcsimOutput = gcsim.Exec(tempSimPath);
                    weaponDpsParams = GetWeaponDps(gcsimOutput, settings.CharacterName);
                }
                catch (Exception ge)
                {
                    // If calculation fails, log and continue with 0 DPS
                    Console.WriteLine("ERROR: Execution Failed.");
                    Console.WriteLine($"Exception: {ge.Message}");
                    Console.WriteLine($"DEBUG: tempSimPath={tempSimPath}");
                    Console.WriteLine("----- GENERATED CONFIG START -----");
                    Console.WriteLine(replacedContent);
                    Console.WriteLine("----- GENERATED CONFIG END -----");
                    if (!string.IsNullOrEmpty(gcsimOutput)) {
                         Console.WriteLine("----- GCSIM OUTPUT START -----");
                         Console.WriteLine(gcsimOutput);
                         Console.WriteLine("----- GCSIM OUTPUT END -----");
                    }
                    Console.WriteLine("DEBUG: Caught exception from Gcsim.Exec, continuing with DPS=0");
                }

                string formattedDps = weaponDpsParams[0];
                if (decimal.TryParse(weaponDpsParams[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                {
                    formattedDps = d.ToString("0.##", CultureInfo.InvariantCulture);
                }
                
                Console.WriteLine($"{weapon.NameInternal}{critSuffix} Char DPS:{formattedDps}");

                lock (tableLock)
                {
                    table.Rows.Add(weapon.NameJapanese, weaponRefineRank, formattedDps);
                    Console.WriteLine($"DEBUG: Added row for {weapon?.NameInternal} DPS={formattedDps}");
                }

                // Cleanup
                try { _FileManager.DeleteFile(tempSimPath); } catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing weapon {weapon.NameInternal}: {ex.Message}");
            }
        }

        public static string[] GetWeaponDps(string gcsimOutput, string characterName)
        {
            // 1. Try to parse plain-text output
            try
            {
                // Pattern: "<CharacterName> total avg dps: 7198.71"
                string specificPattern = $"{Regex.Escape(characterName)}\\s+total\\s+avg\\s+dps\\s*:\\s*([0-9]+\\.?[0-9]*)";
                Match m = Regex.Match(gcsimOutput, specificPattern, RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    return new string[] { m.Groups[1].Value, "0", m.Groups[1].Value, "0" };
                }
            }
            catch (Exception ex)
            {
                // Intentionally ignore parse errors here and fall back to JSON-style parsing below.
                Debug.WriteLine($"Plain-text gcsim output parsing failed for '{characterName}': {ex}");
            }

            // 2. Fallback: Parse JSON output structure (legacy format)
            MatchCollection nameMatches = JsonNameRegex.Matches(gcsimOutput);
            string characterNameQuery = $"\"name\":\"{characterName}\"";

            int charDpsIdx = 0;
            int charStdIdx = 0;
            int matchCounter = 0;
            bool foundChar = false;

            // Determine index offsets based on where the character name appears in the 'name' fields
            foreach (Match match in nameMatches)
            {
               if (match.Value == characterNameQuery)
                {
                    foundChar = true;
                    switch (matchCounter)
                    {
                        case 0: charDpsIdx = 2; charStdIdx = 3; break;
                        case 2: charDpsIdx = 6; charStdIdx = 7; break;
                        case 4: charDpsIdx = 10; charStdIdx = 11; break;
                        case 6: charDpsIdx = 14; charStdIdx = 15; break;
                        default: throw new Exception(Message.Error.GcsimOutputNone);
                    }
                    break;
                }
               matchCounter++;
            }
            
            if (!foundChar && nameMatches.Count > 0)
            {
                // fallback if exact match logic failed but output exists
                 throw new Exception(Message.Error.GcsimOutputNone);
            }

            // Extract numeric sections
            int posTeamDpsHead = gcsimOutput.IndexOf("dps") + 6;
            int posTeamDpsTail = Math.Min(posTeamDpsHead + 1500, gcsimOutput.Length);
            
            int posCharDpsHead = gcsimOutput.IndexOf("character_dps") + 17;
            int posCharDpsTail = Math.Min(posCharDpsHead + 1500, gcsimOutput.Length);

            if (posTeamDpsHead < 6 || posCharDpsHead < 17) // Check if not found (-1 + offset)
                 throw new Exception(Message.Error.GcsimOutputNone);

            string[] teamDpsArray = gcsimOutput.Substring(posTeamDpsHead, posTeamDpsTail - posTeamDpsHead).Split(',');
            string[] charDpsArray = gcsimOutput.Substring(posCharDpsHead, posCharDpsTail - posCharDpsHead).Split(',');

            string charDps = NumberRegex.Match(charDpsArray[charDpsIdx]).Value;
            string charStd = NumberRegex.Match(charDpsArray[charStdIdx]).Value;
            string teamDps = NumberRegex.Match(teamDpsArray[2]).Value;
            string teamStd = NumberRegex.Match(teamDpsArray[3]).Value;

            return new string[] { charDps, charStd, teamDps, teamStd };
        }

        public static string ReplaceText(string content, string oldText, string newText)
        {
            // Optimized: Use string.Replace directly instead of splitting lines.
            return content.Replace(oldText, newText);
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
            // string WeaponType = Console.ReadLine() ?? "";
            // Fix: Do not block for WeaponType input. Let Main resolve it.
            string WeaponType = "TBD"; 

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

            // 1. Try to read the result file specified by -out flag
            if (File.Exists(outputPath))
            {
                try
                {
                    string fileOutput = File.ReadAllText(outputPath);
                    if (!string.IsNullOrWhiteSpace(fileOutput))
                    {
                        try { File.Delete(outputPath); } catch { }
                        return fileOutput;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG: Failed to read output file: {ex.Message}");
                }
            }

            // 2. Fallback to process stdout/stderr (used by tests/mocks or if file failed)
            string? procOutput = proc.GetOuptput();
            if (!string.IsNullOrEmpty(procOutput))
            {
                Debug.WriteLine(procOutput);
                // If output file exists but somehow we fell through here, try delete it
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
            throw new System.NotSupportedException(
                "Exec(IProcessFactory) is no longer supported. Call Exec(string tempSimConfigPath, IProcessFactory?) with a valid configuration path.");
        }
    }
}