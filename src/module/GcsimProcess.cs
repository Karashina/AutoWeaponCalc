using System.Diagnostics;
using System.Text;
using CalcsheetGenerator.Interfaces;

namespace CalcsheetGenerator.Module
{
    public class GcsimProcess : IGcsimProcess
    {
        private Process _Process;
        private string[] _Args;
        private object _Lock = new object();

        public GcsimProcess(string[] args)
        {
            _Args = args;
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = args[0];
            var sb = new StringBuilder();
            for (int i = 1; i < args.Length; i++) {
                sb.Append(args[i]);
                sb.Append(' ');
            }
            startInfo.Arguments = sb.ToString().TrimEnd();
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            _Process = new Process();
            _Process.StartInfo = startInfo;
        }

        public void Start()
        {
            lock (_Lock)
            {
                _Process.Start();
            }
        }

        public void WaitForExit()
        {
            _Process.WaitForExit();
        }

        public String GetOutput()
        {
            var output = _Process.StandardOutput.ReadToEnd();
            var err = _Process.StandardError.ReadToEnd();
            return output + err;
        }
        public string? GetOuptput()
        {
            return GetOutput();
        }
    }
}