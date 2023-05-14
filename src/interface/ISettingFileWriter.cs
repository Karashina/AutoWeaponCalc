using System.Data;

namespace CalcsheetGenerator.Interfaces
{
    interface ISettingFileWriter
    {

        public void WriteText(string FilePath, bool Append, string TextContent, IStreamWriter _StreamWriter);

        public void ReplaceText(string filename, string oldtext, string newtext, IStreamWriter _StreamWriter);

        public void ExportDataTableToCsv(DataTable OutputDataTable, string CsvFileName, IStreamWriter _StreamWriter);
    }

}