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

namespace ConsoleApp1
{
    class GetCharacterDPS
    {

        //1行分のデータを格納するレコード
        record Data(string h1, string h2, string h3);

        public static void Main(string[] args)
        {
            //キャラ名指定
            Console.WriteLine("Type the name of the character to calculate:");
            string? v1 = Console.ReadLine();
            if (v1 == null)
            {
                Environment.Exit(0);
            }
            string charinput = v1;

            //武器種指定
            Console.WriteLine("Type the weapon type of the character to calculate [sword|claymore|bow|catalyst|polearm] :");
            string? v2 = Console.ReadLine();
            if (v2 == null)
            {
                Environment.Exit(0);
            }
            string weptypeinput = v2;

            //精錬ランク指定
            Console.WriteLine("Type the refinement rank of the weapon to calculate [0=auto][1-5] :");
            string? v3 = Console.ReadLine();
            if (v3 == null)
            {
                Environment.Exit(0);
            }
            string refineinput = v3;

            //精錬ランク指定
            Console.WriteLine("Do you want to use artifact mode? [y|n]:");
            string? v4 = Console.ReadLine();
            if (v4 == null)
            {
                Environment.Exit(0);
            }
            string artifactmode = v4;

            //最後に出力する表を作成
            DataSet dataSet = new DataSet();
            DataTable table = new DataTable("Table");

            //カラム名の追加
            table.Columns.Add("武器名");
            table.Columns.Add("精錬R");
            table.Columns.Add("DPS");

            //DataSetにDataTableを追加
            dataSet.Tables.Add(table);

            //---CSV読み込み部分---

            //一時的に武器名を格納する表を作成
            DataSet wdataSet = new DataSet();
            DataTable wtable = new DataTable("Table");

            // カラム名の追加
            wtable.Columns.Add("wepname");
            wtable.Columns.Add("rarity");

            // DataSetにDataTableを追加
            wdataSet.Tables.Add(wtable);

            //ファイル名
            var fileName = weptypeinput + ".csv";

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

            //各武器の計算をする
            foreach (var line in lines)
            {
                wtable.Rows.Add(line.h2, line.h3);
                string wname = line.h2;
                string wnamejp = line.h1;
                string refine;

                //自動精錬ランク設定
                if (refineinput == "0")
                {
                    if (line.h3 == "0")
                    {
                        refine = "5";
                    }
                    else if (line.h3 == "1")
                    {
                        refine = "1";
                    }
                    else
                    {
                        refine = refineinput;
                    }
                }
                else
                {
                    refine = refineinput;
                }

                string charname = charinput;
                txtwriter(wname, charname, refine, false);
                float DPS = getDPS(charname);
                Console.WriteLine(wname + ":" + DPS);
                table.Rows.Add(wnamejp, refine, DPS);
                txtwriter(wname, charname, refine, true);
            }

            DataTableToCsv(table, "table.csv", true);
        }

        public static void txtwriter(string wname, string charname, string refine, bool cleanup)
        {
            string filename = "config.txt";
            string oldtextwep = charname + " add weapon=\"<weapon>\" refine=<refine>";
            string newtextwep = charname + " add weapon=" + "\"" + wname + "\"" + " refine=" + refine;

            if (cleanup)
            {
                //クリーンアップモード
                oldtextwep = charname + " add weapon=" + "\"" + wname + "\"" + " refine=" + refine;
                newtextwep = charname + " add weapon=\"<weapon>\" refine=<refine>";
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
        }

        public static float getDPS(string charname)
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
                Console.WriteLine("DPS:" + cDPS);

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