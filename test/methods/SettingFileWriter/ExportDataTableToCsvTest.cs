using System.Text;
using System.Data;

using CalcsheetGenerator;
using CalcsheetGenerator.Interfaces;

namespace Test.Methods
{
    public class ExportDataTableToCsvTest
    {
        static readonly SettingFileWriter _SettingFileWriter = SettingFileWriter.GetInstance();

        [Fact(DisplayName="書き込み処理が行われること")]
        public void Nomal()
        {
            DataTable OutputDataTable = new DataTable("Table");
            OutputDataTable.Columns.Add("武器名");
            OutputDataTable.Columns.Add("精錬R");
            OutputDataTable.Columns.Add("DPS");
            OutputDataTable.Rows.Add("霧切の廻光","1","14559.18");
            OutputDataTable.Rows.Add("風鷹剣","1","12310.88");
            OutputDataTable.Rows.Add("斬山の刃","1","13008.99");
            OutputDataTable.Rows.Add("天空の刃","1","13570.74");
            Mock<IStreamWriter> MockStreamWriter = new Mock<IStreamWriter>();
            MockStreamWriter.Setup(sw => sw.WriteLine(It.IsAny<string>())).Callback(() => {});
            Mock<IStreamWriterFactory> MockStreamWriterFactory= new Mock<IStreamWriterFactory>();
            MockStreamWriterFactory.Setup(swf => swf.Create(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Encoding>()))
                .Returns(MockStreamWriter.Object);
            
            _SettingFileWriter.ExportDataTableToCsv(OutputDataTable, "test.csv", MockStreamWriterFactory.Object);

            MockStreamWriter.Verify(sw => sw.WriteLine("武器名,精錬R,DPS"), Times.Once);
            MockStreamWriter.Verify(sw => sw.WriteLine("霧切の廻光,1,14559.18"), Times.Once);
            MockStreamWriter.Verify(sw => sw.WriteLine("風鷹剣,1,12310.88"), Times.Once);
            MockStreamWriter.Verify(sw => sw.WriteLine("斬山の刃,1,13008.99"), Times.Once);
            MockStreamWriter.Verify(sw => sw.WriteLine("天空の刃,1,13570.74"), Times.Once);
        }
    }
}