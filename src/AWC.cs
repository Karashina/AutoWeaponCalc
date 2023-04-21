using System.Data;
using System.Diagnostics;
using System.Text;

namespace CalcsheetGenerator
{
    class AWC
    {
        //データを格納するレコード
        record WeaponData(string NameJapanese, string NameInternal, string Rarity);
        record ArtifactData(string PiecesCheck, string Name1, string Name2);

        public static void Main()
        {
            //起動時設定
            bool isAutoModeEnabled = StartupAutoswitch();

            //設定取得
            string[] InitialSetting = new string[4];
            InitialSetting[0] = "";

            if (isAutoModeEnabled)
            {
                InitialSetting = StartupAutomode();
            }
            else
            {
                InitialSetting = StartupManualmode();
            }

            //nullが入ったら強制終了
            if (InitialSetting.Contains(null))
            {
                throw new FormatException("Invalid Input at Automode Startup!");
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
            if (InitialSetting[3] == "y")//setting[3]は聖遺物モード切替
            {
                DpsCalcWithArtifactMode(OutputDataTable, InitialSetting[0], InitialSetting[1], InitialSetting[2]);
            }
            else
            {
                MakeWeaponDpsList(false, OutputDataTable, InitialSetting[0], InitialSetting[1], InitialSetting[2], true, "", "");
            }

        }

        public static bool StartupAutoswitch()
        {
            //モード指定(auto / manual)
            Console.WriteLine("mode selection(auto / manual) [a|m] :");
            string? UserInputModeSelection = Console.ReadLine();

            switch (UserInputModeSelection)
            {
                case "a":
                    return true;
                case "m":
                    return false;
                default:
                    throw new FormatException("Invalid Input at Mode Selection!");
            }
        }

        public static string[] StartupAutomode() //autoモード
        {
            //キャラ名指定
            Console.WriteLine("Type the name of the character to calculate:");
            string? UserInputCharacterName = Console.ReadLine();

            //武器種指定
            Console.WriteLine("Type the weapon type of the character to calculate [sword|claymore|bow|catalyst|polearm] :");
            string? UserInputWeaponType = Console.ReadLine();

            //配列にして返す
            string[] InitialSetting = new string[4] { UserInputCharacterName, UserInputWeaponType, "0", "y" };

            return InitialSetting;
        }

        public static string[] StartupManualmode()//manualモード
        {
            //キャラ名指定
            Console.WriteLine("Type the name of the character to calculate:");
            string? UserInputCharacterName = Console.ReadLine();

            //武器種指定
            Console.WriteLine("Type the weapon type of the character to calculate [sword|claymore|bow|catalyst|polearm] :");
            string? UserinputWeaponType = Console.ReadLine();

            //精錬ランク指定
            Console.WriteLine("Type the refinement rank of the weapon to calculate [0=auto][1-5] :");
            string? UserinputWeaponRefinerank = Console.ReadLine();

            //聖遺物モード切替
            Console.WriteLine("Do you want to use artifact mode? [y|n]:");
            string? UserinputArtifactModeSel = Console.ReadLine();

            //配列にして返す
            string[] InitialSetting = new string[4] { UserInputCharacterName, UserinputWeaponType, UserinputWeaponRefinerank, UserinputArtifactModeSel };

            return InitialSetting;
        }

        public static void DpsCalcWithArtifactMode(DataTable OutputDataTable, string CharacterName, string WeaponType, string WeaponRefinerank)//CSV読み込みと計算
        {
            //ファイル名
            string CsvPathArtifact = "../resource/input/artifacts.csv";

            //先頭行を読み取りするかどうか
            bool isFirstLineSkip = true;

            //取得したデータを保存するリスト
            List<ArtifactData> ArtifactList = new List<ArtifactData>();

            try
            {
                //ファイルを開く
                using (StreamReader CsvReader = new StreamReader(CsvPathArtifact))
                {
                    while (0 <= CsvReader.Peek())
                    {
                        //カンマ区切りで分割して配列で格納する
                        string[] Column = CsvReader.ReadLine()?.Split(',');
                        if (Column is null) continue;

                        //先頭行は項目名なのでスキップする
                        if (isFirstLineSkip)
                        {
                            isFirstLineSkip = false;
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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


            foreach (ArtifactData Artifact in ArtifactList)
            {
                bool isArtifact4Piece = Artifact.PiecesCheck == "1";

                Console.WriteLine("Initialize calculation for artifact " + Artifact.Name1 + Artifact.Name2); //開始メッセージ
                MakeWeaponDpsList(true, OutputDataTable, CharacterName, WeaponType, WeaponRefinerank, isArtifact4Piece, Artifact.Name1, Artifact.Name2);
                OutputDataTable.Clear();//次の聖遺物のため書き出し用リストを初期化
                Console.WriteLine("Calculation completed for artifact " + Artifact.Name1 + Artifact.Name2); //終了メッセージ
            }
        }

        public static void MakeWeaponDpsList(bool isAritifactModeEnabled, DataTable OutputDataTable, string CharacterName, string WeaponType, string WeaponRefinerank, bool isArtifact4Piece, string ArtifactName1, string ArtifactName2) //CSV読み込みと計算（武器）
        {
            //ファイル名
            var CsvPathWeapon = "../resource/weaponData/" + WeaponType + ".csv";

            //先頭行を読み取りするかどうか
            var isFirstLineSkip = true;

            //取得したデータを保存するリスト
            List<WeaponData> WeaponList = new List<WeaponData>();

            try//CSV読み込み部分
            {
                //ファイルを開く
                using (StreamReader CsvReader = new StreamReader(CsvPathWeapon))
                {
                    while (0 <= CsvReader.Peek())
                    {
                        //カンマ区切りで分割して配列で格納する
                        var Column = CsvReader.ReadLine()?.Split(',');
                        if (Column is null) continue;

                        //先頭行は項目名なのでスキップする
                        if (isFirstLineSkip)
                        {
                            isFirstLineSkip = false;
                            continue;
                        }

                        //リストにデータを追加する
                        WeaponList.Add(new WeaponData(Column[0], Column[1], Column[2]));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            foreach (WeaponData Weapon in WeaponList)
            {
                bool isAutoRefineModeEnabled = false;//自動精錬ランク設定初期化

                //rarityに応じた自動精錬ランク設定
                if (WeaponRefinerank == "0")
                {
                    isAutoRefineModeEnabled = true;

                    if (Weapon.Rarity == "1")
                    {
                        WeaponRefinerank = "1";
                    }
                    else
                    {
                        WeaponRefinerank = "5";
                    }
                }

                EditTxtConfig(isAritifactModeEnabled, Weapon.NameInternal, CharacterName, WeaponRefinerank, false, isArtifact4Piece, ArtifactName1, ArtifactName2); //configファイル編集

                float WeaponDps = GetWeaponDps(CharacterName); //gcsim起動
                Console.WriteLine(Weapon.NameInternal + ":" + WeaponDps); //Consoleに進捗出力

                OutputDataTable.Rows.Add(Weapon.NameJapanese, WeaponRefinerank, WeaponDps); //tableに結果を格納

                EditTxtConfig(isAritifactModeEnabled, Weapon.NameInternal, CharacterName, WeaponRefinerank, true, isArtifact4Piece, ArtifactName1, ArtifactName2); //configファイルを元に戻す

                if (isAutoRefineModeEnabled == true)//自動精錬ランク設定を次の武器に引き継ぐ
                {
                    WeaponRefinerank = "0";
                }
            }

            DataTableToCsv(OutputDataTable, "table_" + ArtifactName1 + ArtifactName2 + ".csv", true);
            WeaponList.Clear();//念のためlinesもクリア
        }

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

        static public void DataTableToCsv(DataTable OutputDataTable, string CsvFileName, bool CsvHeader)
        {
            string Separator = string.Empty;
            List<int> filterIndex = new List<int>();

            using (StreamWriter CsvWriter = new StreamWriter(CsvFileName, false, Encoding.UTF8))
            {
                //DataColumnの型から値を出力するかどうか判別
                for (int i = 0; i < OutputDataTable.Columns.Count; i++)
                {
                    switch (OutputDataTable.Columns[i].DataType.ToString())
                    {
                        case "System.Boolean":
                        case "System.Byte":
                        case "System.Char":
                        case "System.DateTime":
                        case "System.Decimal":
                        case "System.Double":
                        case "System.Int16":
                        case "System.Int32":
                        case "System.Int64":
                        case "System.SByte":
                        case "System.Single":
                        case "System.String":
                        case "System.TimeSpan":
                        case "System.UInt16":
                        case "System.UInt32":
                        case "System.UInt64":
                            break;

                        default:
                            filterIndex.Add(i);
                            break;
                    }
                }
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
                        if (filterIndex.Contains(i))
                        {
                            CsvWriter.Write(Separator + "\"[データ]\"");
                            Separator = ",";
                        }
                        else
                        {
                            CsvWriter.Write(Separator + "\"" + CsvRow[i].ToString().Replace("\"", "\"\"") + "\"");
                            Separator = ",";
                        }
                    }
                    CsvWriter.WriteLine();
                }
            }
        }

    }
}
