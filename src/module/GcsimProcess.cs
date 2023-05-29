using System.Diagnostics;

using CalcsheetGenerator.Interfaces;

namespace CalcsheetGenerator.Module
{
    public partial class GcsimProcess : IGcsimProcess
    {
        private Process? _Process;

        public GcsimProcess(string[] args)
        {
            this._Process = new Process();
            this._Process.StartInfo = new ProcessStartInfo {
                FileName = args[0],
                Arguments = String.Join(" ", args.Skip(1).ToArray()),
                CreateNoWindow = true, // ウィンドウを表示しない
                UseShellExecute = false, // ウィンドウを表示しない
                RedirectStandardError = true, // 標準出力および標準エラー出力を取得可能にする
                RedirectStandardOutput = true // 標準出力および標準エラー出力を取得可能にする
            };
        }

        public virtual void Start()
        {
            this._Process?.Start();
        }

        public virtual void WaitForExit()
        {
            this._Process?.WaitForExit();
        }

        public virtual string? GetOuptput()
        {
            return this._Process?.StandardOutput.ReadToEnd();
        }
    }
}