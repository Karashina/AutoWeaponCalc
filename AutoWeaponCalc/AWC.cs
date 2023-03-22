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
    class Primary
    {
        //データを格納するレコード
        record Data(string w1, string w2, string w3);
        record artiData(string a1, string a2, string a3);

        public static void Main(string[] args)
        {
            //起動時設定
            bool auto = startup_autoswitch();

            //設定取得
            string[] setting = new string[4];
            setting[0] = "";
            if (auto)
            {
                setting = startup_automode();
            }
            else
            {
                setting = startup_manualmode();
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

            //CSV読み込み
            if (setting[3] == "y")
            {
               withartifactmode(tableOut, setting[0], setting[1], setting[2]);
            }
            else
            {
                weaponcalc(false, tableOut, setting[0], setting[1], setting[2], true, "", "");
            }

        }

        public static bool startup_autoswitch()
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

        public static string[] startup_automode() //autoモード
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
            if (new string[] { a1, a2 }.Contains(null))
            {
                Environment.Exit(0);
            }

            //配列にして返す
            string[] auto_settingstoreturn = new string[4] { a1, a2, "0", "y" };
            return auto_settingstoreturn;
        }

        public static string[] startup_manualmode()//manualモード
        {
            //キャラ名指定
            Console.WriteLine("Type the name of the character to calculate:");
            string? v1 = Console.ReadLine();
            string charinput = v1;

            //武器種指定
            Console.WriteLine("Type the weapon type of the character to calculate [sword|claymore|bow|catalyst|polearm] :");
            string? v2 = Console.ReadLine();
            string weptypeinput = v2;

            //精錬ランク指定
            Console.WriteLine("Type the refinement rank of the weapon to calculate [0=auto][1-5] :");
            string? v3 = Console.ReadLine();
            string refineinput = v3;

            //聖遺物モード切替
            Console.WriteLine("Do you want to use artifact mode? [y|n]:");
            string? v4 = Console.ReadLine();
            string artifactmode = v4;

            //nullが入ったら強制終了
            if (new string[] { v1 , v2 , v3 , v4 }.Contains(null))
            {
                Environment.Exit(0);
            }

            //配列にして返す
            string[] man_settingstoreturn = new string[4] { v1, v2, v3, v4 }; 
            return man_settingstoreturn;
        }

        public static void withartifactmode(DataTable tb, string ch, string wt, string rf)//CSV読み込みと計算
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
            var fileName = "artifacts.csv";

            //先頭行を読み取りするかどうか
            var isFirstLineSkip = true;

            //取得したデータを保存するリスト
            var lines = new List<artiData>();

            try
            {
                //ファイルを開く
                using (StreamReader sr = new StreamReader(fileName))
                {
                    while (0 <= sr.Peek())
                    {
                        //カンマ区切りで分割して配列で格納する
                        var line = sr.ReadLine()?.Split(',');
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
                string is4pcstr = line.a1;
                string aname1 = line.a2;
                string aname2 = line.a3;

                bool is4pc = false;//4セット分岐

                if (is4pcstr == "1")
                {
                    is4pc = true;
                    aname2 = "";
                }

                weaponcalc(true, tb, ch, wt, rf, is4pc, aname1, aname2);
                Console.WriteLine("Calculation completed for artifact " + aname1 + aname2); //Consoleに進捗出力
            }
        }

        public static void weaponcalc(bool amode, DataTable table, string character, string weptype, string refine, bool i4p, string a1, string a2)//CSV読み込みと計算
        {
            
            //一時的に武器名を格納する表を作成
            DataSet wdataSet = new DataSet();
            DataTable weptable = new DataTable("WTable");

            // カラム名の追加
            weptable.Columns.Add("wepname");
            weptable.Columns.Add("rank");
            weptable.Columns.Add("dps");

            // DataSetにDataTableを追加
            wdataSet.Tables.Add(weptable);

            //ファイル名
            var fileName = weptype + ".csv";

            //先頭行を読み取りするかどうか
            var isFirstLineSkip = true;

            //取得したデータを保存するリスト
            var lines = new List<Data>();

            try
            {
                //ファイルを開く
                using (StreamReader sr = new StreamReader(fileName))
                {
                    while (0 <= sr.Peek())
                    {
                        //カンマ区切りで分割して配列で格納する
                        var line = sr.ReadLine()?.Split(',');
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
                weptable.Rows.Add(line.w2, line.w3);
                string wname = line.w2;
                string wnamejp = line.w1;
                

                //rarityに応じた自動精錬ランク設定
                if (refine == "0")
                {
                    if (line.w3 == "0")
                    {
                        refine = "5";
                    }
                    else
                    {
                        refine = "1";
                    }
                }

                txtwriter(amode, wname, character, refine, false, i4p, a1, a2); //configファイル編集
                float DPS = getDPS(character); //gcsim起動
                Console.WriteLine(wname + ":" + DPS); //Consoleに進捗出力
                table.Rows.Add(wnamejp, refine, DPS); //tableに結果を格納
                txtwriter(amode, wname, character, refine, true, i4p, a1, a2); //configファイルを元に戻す
            }

            DataTableToCsv(table, "table_" + a1 + a2 + ".csv", true);
        }

        public static void txtwriter(bool am, string wname, string charname, string refine, bool cleanup, bool is4pc, string aname1, string aname2)
        {
            string filename = "config.txt";
            string oldtextwep = charname + " add weapon=\"<w>\" refine=<r>";
            string newtextwep = charname + " add weapon=" + "\"" + wname + "\"" + " refine=" + refine;
            
            if (cleanup == true)
            {
                //クリーンアップモード
                oldtextwep = charname + " add weapon=" + "\"" + wname + "\"" + " refine=" + refine;
                newtextwep = charname + " add weapon=\"<w>\" refine=<r>";
                StringBuilder strread = new StringBuilder();
                string[] strarray = File.ReadAllLines(filename, Encoding.UTF8);
                for (int i = 0; i < strarray.GetLength(0); i++)
                {
                    if (strarray[i].Contains(oldtextwep) == true)
                    {
                        strread.AppendLine(strarray[i].Replace(oldtextwep, newtextwep));
                    }
                    else
                    {
                        strread.AppendLine(strarray[i]);
                    }
                }
                File.WriteAllText(filename, strread.ToString());
                Debug.WriteLine("Cleaned");
            }
            else
            {
                //置き換えモード
                StringBuilder strread = new StringBuilder();
                string[] strarray = File.ReadAllLines(filename, Encoding.UTF8);
                for (int i = 0; i < strarray.GetLength(0); i++)
                {
                    if (strarray[i].Contains(oldtextwep) == true)
                    {
                        strread.AppendLine(strarray[i].Replace(oldtextwep, newtextwep));
                    }
                    else
                    {
                        strread.AppendLine(strarray[i]);
                    }
                }
                File.WriteAllText(filename, strread.ToString());
                Debug.WriteLine("Replaced");
            }

            if (am == true)
            {
                string oldtextart = "";
                string newtextart = "";

                if (is4pc == true )//4セットか2セット混合かで分岐
                {
                    //4セット混合
                    oldtextart = charname + " add set=\"<a>\" count=<p>";
                    newtextart = charname + " add set=" + "\"" + aname1 + "\"" + " count=4";
                }
                else
                {
                    //2セット混合
                    oldtextart = charname + " add set=\"<a>\" count=<p>;";
                    newtextart = charname + " add set=" + "\"" + aname1 + "\"" + " count=2;" + Environment.NewLine + charname + " add set=" + "\"" + aname2 + "\"" + " count=2;";
                }

                if (cleanup == true)
                {
                    //クリーンアップモード
                    if (is4pc == true)//4セットか2セット混合かで分岐
                    {
                        //4セット混合
                        oldtextart = charname + " add set=" + "\"" + aname1 + "\"" + " count=4";
                        newtextart = charname + " add set=\"<a>\" count=<p>";
                    }
                    else
                    {
                        //2セット混合
                        oldtextart = charname + " add set=" + "\"" + aname1 + "\"" + " count=2;" + Environment.NewLine + charname + " add set=" + "\"" + aname2 + "\"" + " count=2;";
                        newtextart = charname + " add set=\"<a>\" count=<p>;";
                    }

                    StringBuilder strread = new StringBuilder();
                    string[] strarray = File.ReadAllLines(filename, Encoding.UTF8);
                    for (int i = 0; i < strarray.GetLength(0); i++)
                    {
                        if (strarray[i].Contains(oldtextart) == true)
                        {
                            strread.AppendLine(strarray[i].Replace(oldtextart, newtextart));
                        }
                        else
                        {
                            strread.AppendLine(strarray[i]);
                        }
                    }
                    File.WriteAllText(filename, strread.ToString());
                }
                else
                {
                    //置き換えモード
                    StringBuilder strread = new StringBuilder();
                    string[] strarray = File.ReadAllLines(filename, Encoding.UTF8);
                    for (int i = 0; i < strarray.GetLength(0); i++)
                    {
                        if (strarray[i].Contains(oldtextart) == true)
                        {
                            strread.AppendLine(strarray[i].Replace(oldtextart, newtextart));
                        }
                        else
                        {
                            strread.AppendLine(strarray[i]);
                        }
                    }
                    File.WriteAllText(filename, strread.ToString());
                }
            }
        }

        public static float getDPS(string charname)//gcsimで計算
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
            gcsim.StartInfo.FileName = "gcsim.exe";

            // gcsimに渡す引数
            string txtname = "config.txt";
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