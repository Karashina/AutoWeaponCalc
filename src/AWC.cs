using System.Data;
using System.Diagnostics;
using System.Text;

using Common;

namespace CalcsheetGenerator
{
    class Primary
    {
        public static void Main()
        {
            try
            {
                Preparation preparation = new Preparation();

                //起動時設定
                preparation.SelectMode();

                //設定取得
                UserInput InitialSetting = preparation.Startup();

                //nullまたは空文字が入ったら強制終了
                if (InitialSetting.CheckNullAndEmpty())
                {
                    throw new FormatException(Message.Error.StartupAutomode);
                }

                //最後に出力する表を作成
                DataSet OutputDataSet = new DataSet();
                DataTable OutputDataTable = new DataTable("Table");

                //カラム名の追加
                OutputDataTable.Columns.Add("武器名");
                OutputDataTable.Columns.Add("精錬R");
                OutputDataTable.Columns.Add("DPS");

                //DataSetにDataTableを追加
                OutputDataSet.Tables.Add(OutputDataTable);

                //モードごとに処理
                if (InitialSetting.ArtifactModeSel == "y")
                {
                    Calculation.DpsCalcWithArtifactMode(OutputDataTable, InitialSetting);
                }
                else
                {
                    Calculation.MakeWeaponDpsList(false, OutputDataTable, InitialSetting, true, "", "");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(1);
            }
        }
    }
    class Preparation
    {
        Mode mode = Mode.none;
        public void SelectMode()
        {
            //モード指定(auto / manual)
            Console.WriteLine(Message.Notice.SelectMode);
            string? UserInputModeSelection = Console.ReadLine();

            switch (UserInputModeSelection)
            {
                case "a":
                    this.mode = Mode.auto;
                    break;
                case "m":
                    this.mode = Mode.manual;
                    break;
                default:
                    throw new FormatException(Message.Error.SelectMode);
            }
        }

        public bool IsAutoMode()
        {
            return Mode.auto.Equals(this.mode);
        }

        public UserInput Startup()//manualモード
        {
            if (Mode.none.Equals(this.mode)){
                // TODO throw
            }
            string? WeaponRefinerank = "0";
            string? ArtifactModeSel = "y";

            //キャラ名指定
            Console.WriteLine(Message.Notice.SelectCharctor);
            string? CharacterName = Console.ReadLine();

            //武器種指定
            Console.WriteLine(Message.Notice.SelectWeapon);
            string? WeaponType = Console.ReadLine();

            if (Mode.manual.Equals(this.mode))
            {
                //精錬ランク指定
                Console.WriteLine(Message.Notice.SelectRefinement);
                WeaponRefinerank = Console.ReadLine();

                //聖遺物モード切替
                Console.WriteLine(Message.Notice.SelectArtifactOptimization);
                ArtifactModeSel = Console.ReadLine();
            }

            return new UserInput(CharacterName, WeaponType, WeaponRefinerank, ArtifactModeSel);
        }
    }

    class Calculation
    {
        //データを格納するレコード
        record WeaponData(string NameJapanese, string NameInternal, string Rarity);
        record ArtifactData(string PiecesCheck, string Name1, string Name2);
        public static void MakeWeaponDpsList(bool isAritifactModeEnabled, DataTable OutputDataTable, UserInput InitialSetting, bool isArtifact4Piece, string ArtifactName1, string ArtifactName2) //CSV読み込みと計算（武器）
        {
            //ファイル名
            string CsvPathWeapon = "../resource/weaponData/" + InitialSetting.WeaponType + ".csv";

            //先頭行を読み取りするかどうか
            bool isCsvHeader = true;

            //取得したデータを保存するリスト
            List<WeaponData> WeaponList = new List<WeaponData>();

            //CSV読み込み部分
            using (StreamReader WeaponCsvReader = new StreamReader(CsvPathWeapon))
            {
                while (0 <= WeaponCsvReader.Peek())
                {
                    //カンマ区切りで分割して配列で格納する
                    string[] Column = WeaponCsvReader.ReadLine()?.Split(',');
                    if (Column is null) continue;

                    //先頭行は項目名なのでスキップする
                    if (isCsvHeader)
                    {
                        isCsvHeader = false;
                        continue;
                    }

                    //リストにデータを追加する
                    WeaponList.Add(new WeaponData(Column[0], Column[1], Column[2]));
                }
            }
            foreach (WeaponData Weapon in WeaponList)
            {
                bool isAutoRefineModeEnabled = false;//自動精錬ランク設定初期化

                //rarityに応じた自動精錬ランク設定
                if (InitialSetting.WeaponRefinerank == "0")
                {
                    isAutoRefineModeEnabled = true;

                    if (Weapon.Rarity == "1")
                    {
                        InitialSetting.WeaponRefinerank = "1";
                    }
                    else
                    {
                        InitialSetting.WeaponRefinerank = "5";
                    }
                }

                FileIO.EditTxtConfig(isAritifactModeEnabled, Weapon.NameInternal, InitialSetting.CharacterName, InitialSetting.WeaponRefinerank, false, isArtifact4Piece, ArtifactName1, ArtifactName2); //configファイル編集

                float WeaponDps = Gcsim.GetWeaponDps(InitialSetting.CharacterName); //gcsim起動
                Console.WriteLine(Weapon.NameInternal + ":" + WeaponDps); //Consoleに進捗出力

                OutputDataTable.Rows.Add(Weapon.NameJapanese, InitialSetting.WeaponRefinerank, WeaponDps); //tableに結果を格納

                FileIO.EditTxtConfig(isAritifactModeEnabled, Weapon.NameInternal, InitialSetting.CharacterName, InitialSetting.WeaponRefinerank, true, isArtifact4Piece, ArtifactName1, ArtifactName2); //configファイルを元に戻す

                if (isAutoRefineModeEnabled == true)//自動精錬ランク設定を次の武器に引き継ぐ
                {
                    InitialSetting.WeaponRefinerank = "0";
                }
            }

            FileIO.DataTableToCsv(OutputDataTable, "table_" + ArtifactName1 + ArtifactName2 + ".csv", true);
            WeaponList.Clear();//念のためlinesもクリア
        }
        public static void DpsCalcWithArtifactMode(DataTable OutputDataTable, UserInput InitialSetting)//CSV読み込みと計算
        {
            //ファイル名
            string CsvPathArtifact = "../resource/input/artifacts.csv";

            //先頭行を読み取りするかどうか
            bool isCsvHeader = true;

            //取得したデータを保存するリスト
            List<ArtifactData> ArtifactList = new List<ArtifactData>();

            //ファイルを開く
            using (StreamReader ArtifactCsvReader = new StreamReader(CsvPathArtifact))
            {
                while (0 <= ArtifactCsvReader.Peek())
                {
                    //カンマ区切りで分割して配列で格納する
                    string[] Column = ArtifactCsvReader.ReadLine()?.Split(',');
                    if (Column is null) continue;

                    //先頭行スキップ
                    if (isCsvHeader)
                    {
                        isCsvHeader = false;
                        continue;
                    }

                    //聖遺物2スロット目を使わない場合出力時に"〇〇4pc"となるようにする
                    if (Column[2] == "0")
                    {
                        Column[2] = "4pc";
                    }

                    //tableにデータを追加する
                    ArtifactList.Add(new ArtifactData(Column[0], Column[1], Column[2]));
                }
            }
            foreach (ArtifactData Artifact in ArtifactList)
            {
                bool isArtifact4Piece = Artifact.PiecesCheck == "1";

                Console.WriteLine($"{Message.Notice.ProcessStart}{Artifact.Name1} {Artifact.Name2}"); //開始メッセージ
                MakeWeaponDpsList(true, OutputDataTable, InitialSetting, isArtifact4Piece, Artifact.Name1, Artifact.Name2);
                OutputDataTable.Clear();//次の聖遺物のため書き出し用リストを初期化
                Console.WriteLine($"{Message.Notice.ProcessEnd}{Artifact.Name1} {Artifact.Name2}"); //終了メッセージ
            }
        }
    }
    class FileIO
    {
        public static void TxtReplace(string filename, string oldtext, string newtext) //txtファイルの内容を置き換える
        {
            StringBuilder TxtBuilder = new StringBuilder();
            string[] TxtLine = File.ReadAllLines(filename, Encoding.UTF8);
            for (int i = 0; i < TxtLine.GetLength(0); i++)
            {
                if (TxtLine[i].Contains(oldtext) == true)
                {
                    TxtBuilder.AppendLine(TxtLine[i].Replace(oldtext, newtext));
                }
                else
                {
                    TxtBuilder.AppendLine(TxtLine[i]);
                }
            }
            File.WriteAllText(filename, TxtBuilder.ToString());
        }
        public static void EditTxtConfig(bool isArtifactModeEnabled, string WeaponName, string CharacterName, string WeaponRefineRank, bool isCleanupModeEnabled, bool isArtifact4Piece, string ArtifactName1, string ArtifactName2) //txtファイルに書き込む内容を指定する
        {
            string TxtPathSimconfig = "../resource/input/config.txt";
            string OldTextWeapon = CharacterName + " add weapon=\"<w>\" refine=<r>";
            string NewTextWeapon = CharacterName + " add weapon=" + "\"" + WeaponName + "\"" + " refine=" + WeaponRefineRank;

            string OldTextArtifact = CharacterName + " add set=\"<a>\" count=<p>;";//置き換え前の文章（聖遺物）
            string NewTextArtifact;//変数だけ作っておく

            if (isArtifact4Piece == true)//聖遺物モード:4セットか2セット混合かで分岐
            {
                //4セット混合
                NewTextArtifact = CharacterName + " add set=" + "\"" + ArtifactName1 + "\"" + " count=4;";//置き換え後の文章（聖遺物）
            }
            else
            {
                //2セット混合
                NewTextArtifact = CharacterName + " add set=" + "\"" + ArtifactName1 + "\"" + " count=2;" + Environment.NewLine + CharacterName + " add set=" + "\"" + ArtifactName2 + "\"" + " count=2;";//置き換え後の文章（聖遺物）
            }

            if (isArtifactModeEnabled == true)//聖遺物モード
            {
                TxtReplace(TxtPathSimconfig, OldTextArtifact, NewTextArtifact);
            }

            if (isCleanupModeEnabled == true)
            {
                //クリーンアップモード
                TxtReplace(TxtPathSimconfig, NewTextWeapon, OldTextWeapon);

                if (isArtifactModeEnabled == true)
                {
                    TxtReplace(TxtPathSimconfig, NewTextArtifact, OldTextArtifact);
                }

                Debug.WriteLine("Cleaned");
            }
            else
            {
                //置き換えモード
                TxtReplace(TxtPathSimconfig, OldTextWeapon, NewTextWeapon);

                if (isArtifactModeEnabled == true)
                {
                    TxtReplace(TxtPathSimconfig, OldTextArtifact, NewTextArtifact);
                }

                Debug.WriteLine("Replaced");
            }
        }
        static public void DataTableToCsv(DataTable OutputDataTable, string CsvFileName, bool CsvHeader)
        {
            string Separator = string.Empty;
            List<int> filterIndex = new List<int>();

            using (StreamWriter CsvWriter = new StreamWriter(CsvFileName, false, Encoding.UTF8))
            {
                //ヘッダーを出力
                if (CsvHeader)
                {
                    foreach (DataColumn CsvColumn in OutputDataTable.Columns)
                    {
                        CsvWriter.Write(Separator + "\"" + CsvColumn.ToString().Replace("\"", "\"\"") + "\"");
                        Separator = ",";
                    }
                    CsvWriter.WriteLine();
                }
                //内容を出力
                foreach (DataRow CsvRow in OutputDataTable.Rows)
                {
                    Separator = string.Empty;
                    for (int i = 0; i < OutputDataTable.Columns.Count; i++)
                    {
                        CsvWriter.Write(Separator + "\"" + CsvRow[i].ToString().Replace("\"", "\"\"") + "\"");
                        Separator = ",";
                    }
                    CsvWriter.WriteLine();
                }
            }
        }
    }
    class Gcsim
    {
        public static float GetWeaponDps(string CharacterName)//gcsimで計算
        {
            // Processクラスのオブジェクトを作成
            Process Gcsim = new Process();

            // ウィンドウを表示しない
            Gcsim.StartInfo.CreateNoWindow = true;
            Gcsim.StartInfo.UseShellExecute = false;

            // 標準出力および標準エラー出力を取得可能にする
            Gcsim.StartInfo.RedirectStandardOutput = true;
            Gcsim.StartInfo.RedirectStandardError = true;

            // gcsimを起動
            Gcsim.StartInfo.FileName = "../resource/execBinary/gcsim.exe";

            // gcsimに渡す引数
            string txtname = "../resource/input/config.txt";
            Gcsim.StartInfo.Arguments = "-c=" + txtname + " -substatOptim=true -out=OptimizedConfig.txt";

            // プロセス起動1回目
            Gcsim.Start();
            Console.WriteLine("Substat optimization in progress...");

            // 標準出力を取得
            string GcsimOutput = Gcsim.StandardOutput.ReadToEnd();

            // 標準出力を表示
            Debug.WriteLine(GcsimOutput);
            Gcsim.WaitForExit();
            Console.WriteLine("Substat optimization completed");

            // 2回目にgcsimに渡す引数
            Gcsim.StartInfo.Arguments = "-c=OptimizedConfig.txt";

            // プロセス起動2回目
            Gcsim.Start();

            // 標準出力を取得
            GcsimOutput = Gcsim.StandardOutput.ReadToEnd();
            Gcsim.WaitForExit();

            // 標準出力を表示
            Debug.WriteLine(GcsimOutput);

            //エラー分岐
            if (GcsimOutput == "")
            {
                Console.WriteLine("ERROR: unrecognized weapon");
                return 0;//計算不能の場合DPS:0として返す
            }
            else
            {
                //DPS数値検索:頭
                string Query1 = CharacterName + " total avg dps: ";

                //DPS数値部分のみを切り出す
                string WeaponDps = GcsimOutput.Substring(GcsimOutput.IndexOf(Query1)).Replace(Query1, "");

                //DPS数値検索:足
                WeaponDps = WeaponDps.Substring(0, WeaponDps.IndexOf(";"));

                //floatに変換して返す
                return float.Parse(WeaponDps);
            }
        }
    }
}