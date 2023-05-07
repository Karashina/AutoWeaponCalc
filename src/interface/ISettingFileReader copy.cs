using System.Data;

namespace Interfaces
{
    interface ISettingFileWriter
    {
        public abstract void ReplaceText(string filename, string oldtext, string newtext);

        public abstract void ExportDataTableToCsv(DataTable OutputDataTable, string CsvFileName);
    }

}