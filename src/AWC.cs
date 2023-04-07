using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Runtime.Intrinsics.X86;
using System.IO;
using System.Data;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.IO.Enumeration;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace CalcsheetGenerator
{
    public class AWC
    {
        //データを格納するレコード
        record Data(string w1, string w2, string w3);
        record artiData(string a1, string a2, string a3);

        public static void Main(string[] args)
        {
            //起動時設定
            bool auto = Startup_autoswitch();

            //設定取得
            string[] setting = new string[4];
            setting[0] = "";
            if (auto)
            {
                setting = Startup_automode();
            }
            else
            {
                setting = Startup_manualmode();
            }

            //最後に出力する表を作成
            DataSet dataSetOut = new DataSet();
            DataTable tableOut = new DataTable("Table");

            //カラム名の追加
            tableOut.Columns.Add("武器名");
            tableOut.Columns.Add("精錬R");
            tableOut.Columns.Add("DPS");

            //DataSetにDataTableを追加
            dataSetOut.Tables.Add(tableOut);

            //モードごとに処理
            if (setting[3] == "y")//setting[3]は聖遺物モード切替
            {
               Artifactmode(tableOut, setting[0], setting[1], setting[2]);
            }
            else
            {
                Weaponcalc(false, tableOut, setting[0], setting[1], setting[2], true, "", "");
            }

        }

        public static bool Startup_autoswitch() 
        {
            //モード指定(auto / manual)
            Console.WriteLine("mode selection(auto / manual) [a|m] :");
            bool modesel = false;
            string? autoswitch = Console.ReadLine();
            if (autoswitch == "a")
            {
                modesel = true;
            }
            return modesel;
        }

        public static string[] Startup_automode() //autoモード
        {
            //キャラ名指定
            Console.WriteLine("Type the name of the character to calculate:");
            string? a1 = Console.ReadLine();
            string charinput = a1;

            //武器種指定
            Console.WriteLine("Type the weapon type of the character to calculate [sword|claymore|bow|catalyst|polearm] :");
            string? a2 = Console.ReadLine();
            string weptypeinput = a2;

            //nullが入ったら強制終了
            if (new string[] { charinput, weptypeinput }.Contains(null))
            {
                Environment.Exit(0);
            }

            //配列にして返す
            string[] auto_settingstoreturn = new string[4] { charinput, weptypeinput, "0", "y" };
            return auto_settingstoreturn;
        }

        public static string[] Startup_manualmode()//manualモード
        {
            //キャラ名指定
            Console.WriteLine("Type the name of the character to calculate:");
            string? m1 = Console.ReadLine();
            string charinput = m1;

            //武器種指定
            Console.WriteLine("Type the weapon type of the character to calculate [sword|claymore|bow|catalyst|polearm] :");
            string? m2 = Console.ReadLine();
            string weptypeinput = m2;

            //精錬ランク指定
            Console.WriteLine("Type the refinement rank of the weapon to calculate [0=auto][1-5] :");
            string? m3 = Console.ReadLine();
            string refineinput = m3;

            //聖遺物モード切替
            Console.WriteLine("Do you want to use artifact mode? [y|n]:");
            string? m4 = Console.ReadLine();
            string artifactmode = m4;

            //nullが入ったら強制終了
            if (new string[] { charinput , weptypeinput , refineinput , artifactmode }.Contains(null))
            {
                Environment.Exit(0);
            }

            //配列にして返す
            string[] man_settingstoreturn = new string[4] { charinput, weptypeinput, refineinput, artifactmode }; 
            return man_settingstoreturn;
        }

        public static void Artifactmode(DataTable table, string charname, string weapontype, string refine)//CSV読み込みと計算
        {
            DataSet adataSet = new DataSet();
            DataTable artitable = new DataTable("aTable");

            // カラム名の追加
            artitable.Columns.Add("is4pc");
            artitable.Columns.Add("artiname1");
            artitable.Columns.Add("artiname2");

            // DataSetにDataTableを追加
            adataSet.Tables.Add(artitable);

            //ファイル名
            var fileName = "../resource/input/artifacts.csv";

            //先頭行を読み取りするかどうか
            var isFirstLineSkip = true;

            //取得したデータを保存するリスト
            var lines = new List<artiData>();

            try
            {
                //ファイルを開く
                using (StreamReader ReaderForArtimode = new StreamReader(fileName))
                {
                    while (0 <= ReaderForArtimode.Peek())
                    {
                        //カンマ区切りで分割して配列で格納する
                        var line = ReaderForArtimode.ReadLine()?.Split(',');
                        if (line is null) continue;

                        //先頭行は項目名なのでスキップする
                        if (isFirstLineSkip)
                        {
                            isFirstLineSkip = false;
                            continue;
                        }

                        //tableにデータを追加する
                        lines.Add(new artiData(line[0], line[1], line[2]));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            foreach (var line in lines)
            {
                artitable.Rows.Add(line.a1, line.a2, line.a3);
                string is4pcstr = line.a1;//4セットか
                string aname1 = line.a2;//聖遺物名1
                string aname2 = line.a3;//聖遺物名2

                bool is4pc = false;//4セット分岐

                if (is4pcstr == "1")
                {
                    is4pc = true;//string→bool
                    aname2 = "";//聖遺物名2にnullが入らないようにする
                }



                Console.WriteLine("Initialize calculation for artifact " + aname1 + aname2); //開始メッセージ
                Weaponcalc(true, table, charname, weapontype, refine, is4pc, aname1, aname2);
                table.Clear();//次の聖遺物のため書き出し用リストを初期化
                Console.WriteLine("Calculation completed for artifact " + aname1 + aname2); //終了メッセージ
            }
        }

        public static void Weaponcalc(bool amode, DataTable table, string character, string weptype, string refine, bool is4pc, string artiname1, string artiname2) //CSV読み込みと計算（武器）
        {
            //ファイル名
            var fileName = "../resource/weaponData/" + weptype + ".csv";

            //先頭行を読み取りするかどうか
            var isFirstLineSkip = true;

            //取得したデータを保存するリスト
            var lines = new List<Data>();

            try//CSV読み込み部分
            {
                //ファイルを開く
                using (StreamReader ReaderForWeaponmode = new StreamReader(fileName))
                {
                    while (0 <= ReaderForWeaponmode.Peek())
                    {
                        //カンマ区切りで分割して配列で格納する
                        var line = ReaderForWeaponmode.ReadLine()?.Split(',');
                        if (line is null) continue;

                        //先頭行は項目名なのでスキップする
                        if (isFirstLineSkip)
                        {
                            isFirstLineSkip = false;
                            continue;
                        }

                        //リストにデータを追加する
                        lines.Add(new Data(line[0], line[1], line[2]));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            foreach (var line in lines)
            {
                string wnamejp = line.w1;//日本語武器名
                string wname = line.w2;//gcsim内部武器名
                bool autorefinemode = false;//自動精錬ランク設定初期化

                //rarityに応じた自動精錬ランク設定
                if (refine == "0")
                {
                    autorefinemode = true;

                    if (line.w3 == "1")
                    {
                        refine = "1";
                    }
                    else
                    {
                        refine = "5";
                    }
                }

                Txtwriter(amode, wname, character, refine, false, is4pc, artiname1, artiname2); //configファイル編集
                float DPS = GetDPS(character); //gcsim起動
                Console.WriteLine(wname + ":" + DPS); //Consoleに進捗出力
                table.Rows.Add(wnamejp, refine, DPS); //tableに結果を格納
                Txtwriter(amode, wname, character, refine, true, is4pc, artiname1, artiname2); //configファイルを元に戻す

                if(autorefinemode == true)//自動精錬ランク設定を次の武器に引き継ぐ
                {
                    refine = "0";
                }
            }

            DataTableToCsv(table, "table_" + artiname1 + artiname2 + ".csv", true);
            lines.Clear();//念のためlinesもクリア
        }

        public static void Txtreplacer(string filename, string oldtext, string newtext) //txtファイルの内容を置き換える
        {
            StringBuilder strread = new StringBuilder();
            string[] strarray = File.ReadAllLines(filename, Encoding.UTF8);
            for (int i = 0; i < strarray.GetLength(0); i++)
            {
                if (strarray[i].Contains(oldtext) == true)
                {
                    strread.AppendLine(strarray[i].Replace(oldtext, newtext));
                }
                else
                {
                    strread.AppendLine(strarray[i]);
                }
            }
            File.WriteAllText(filename, strread.ToString());
        }

        public static void Txtwriter(bool artifactmode, string weaponname, string charname, string refine, bool cleanup, bool is4pc, string artiname1, string artiname2) //txtファイルに書き込む内容を指定する
        {
            string filename = "../resource/input/config.txt";
            string oldtextwep = charname + " add weapon=\"<w>\" refine=<r>";
            string newtextwep = charname + " add weapon=" + "\"" + weaponname + "\"" + " refine=" + refine;
            
            string oldtextart = charname + " add set=\"<a>\" count=<p>;";//置き換え前の文章（聖遺物）
            string newtextart;//変数だけ作っておく

            if (is4pc == true)//聖遺物モード:4セットか2セット混合かで分岐
            {
                //4セット混合
                newtextart = charname + " add set=" + "\"" + artiname1 + "\"" + " count=4;";//置き換え後の文章（聖遺物）
            }
            else
            {
                //2セット混合
                newtextart = charname + " add set=" + "\"" + artiname1 + "\"" + " count=2;" + Environment.NewLine + charname + " add set=" + "\"" + artiname2 + "\"" + " count=2;";//置き換え後の文章（聖遺物）
            }

            if (artifactmode == true)//聖遺物モード
            {
                Txtreplacer(filename, oldtextart, newtextart);
            }

            if (cleanup == true)
            {
                //クリーンアップモード
                Txtreplacer(filename, newtextwep, oldtextwep);

                if(artifactmode == true)
                {
                    Txtreplacer(filename, newtextart, oldtextart);
                }

                Debug.WriteLine("Cleaned");
            }
            else
            {
                //置き換えモード
                Txtreplacer(filename, oldtextwep, newtextwep);

                if (artifactmode == true)
                {
                    Txtreplacer(filename, oldtextart, newtextart);
                }

                Debug.WriteLine("Replaced");
            }
        }

        public static float GetDPS(string charname)//gcsimで計算
        {
            // Processクラスのオブジェクトを作成
            Process gcsim = new Process();

            // ウィンドウを表示しない
            gcsim.StartInfo.CreateNoWindow = true;
            gcsim.StartInfo.UseShellExecute = false;

            // 標準出力および標準エラー出力を取得可能にする
            gcsim.StartInfo.RedirectStandardOutput = true;
            gcsim.StartInfo.RedirectStandardError = true;

            // gcsimを起動
            gcsim.StartInfo.FileName = "../resource/execBinary/gcsim.exe";

            // gcsimに渡す引数
            string txtname = "../resource/input/config.txt";
            gcsim.StartInfo.Arguments = "-c=" + txtname + " -substatOptim=true -out=optim.txt";

            // プロセス起動1回目
            gcsim.Start();
            Console.WriteLine("Substat optimization in progress...");

            // 標準出力を取得
            string Output = gcsim.StandardOutput.ReadToEnd();

            // 標準出力を表示
            Debug.WriteLine(Output);
            gcsim.WaitForExit();
            Console.WriteLine("Substat optimization completed");

            // 2回目にgcsimに渡す引数
            gcsim.StartInfo.Arguments = "-c=optim.txt";

            // プロセス起動2回目
            gcsim.Start();

            // 標準出力を取得
            Output = gcsim.StandardOutput.ReadToEnd();
            gcsim.WaitForExit();

            // 標準出力を表示
            Debug.WriteLine(Output);

            //エラー分岐
            if (Output == "")
            {
                Console.WriteLine("ERROR: unrecognized weapon");
                return 0;
            }
            else
            {
                //DPS数値検索:頭
                string Char = charname;
                string Query1 = Char + " total avg dps: ";
                string cDPS = Output.Substring(Output.IndexOf(Query1));

                //DPS数値部分のみを切り出す
                cDPS = cDPS.Replace(Query1, "");

                //DPS数値検索:足
                cDPS = cDPS.Substring(0, cDPS.IndexOf(";"));

                gcsim.WaitForExit();

                //floatに変換して返す
                float fDPS = float.Parse(cDPS);
                return fDPS;
            }
        }

        static public void DataTableToCsv(DataTable dt, string filePath, bool header)
        {
            string sp = string.Empty;
            List<int> filterIndex = new List<int>();

            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                //DataColumnの型から値を出力するかどうか判別
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    switch (dt.Columns[i].DataType.ToString())
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
                if (header)
                {
                    foreach (DataColumn col in dt.Columns)
                    {
                        sw.Write(sp + "\"" + col.ToString().Replace("\"", "\"\"") + "\"");
                        sp = ",";
                    }
                    sw.WriteLine();
                }
                //内容を出力
                foreach (DataRow row in dt.Rows)
                {
                    sp = string.Empty;
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        if (filterIndex.Contains(i))
                        {
                            sw.Write(sp + "\"[データ]\"");
                            sp = ",";
                        }
                        else
                        {
                            sw.Write(sp + "\"" + row[i].ToString().Replace("\"", "\"\"") + "\"");
                            sp = ",";
                        }
                    }
                    sw.WriteLine();
                }
            }
        }

    }
}
