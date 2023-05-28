using System.Data;

namespace CalcsheetGenerator.Interfaces
{
    public interface ISettingFileWriter
    {

        public void WriteText(string FilePath, bool Append, string TextContent, IStreamWriterFactory? StreamWriterFactory=null);

        public void ExportDataTableToCsv(DataTable OutputDataTable, string CsvFileName, IStreamWriterFactory? StreamWriterFactory=null);
    }

}