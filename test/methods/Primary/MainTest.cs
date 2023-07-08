using System.Data;
using System.Reflection;

using CalcsheetGenerator;
using CalcsheetGenerator.Common;
using CalcsheetGenerator.Module;
using CalcsheetGenerator.Interfaces;

namespace Test.Methods
{
    [Collection("singletonのためテスト直列化")]
    public class MainTest
    {
        [Fact(DisplayName="聖遺物最適化モードありでの起動")]
        public void NomalArtifactModeEnabledAndAutoRefine()
        {
            FieldInfo? PreparationFieldInfo = typeof(Primary).GetField("_Preparation", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static );
            FieldInfo? SettingFileReaderFieldInfo = typeof(Primary).GetField("_SettingFileReader", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static );
            FieldInfo? SettingFileWriterFieldInfo = typeof(Primary).GetField("_SettingFileWriter", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static );
            FieldInfo? GcsimManagerFieldInfo = typeof(Primary).GetField("_GcsimManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static );
            // Preparation
            Mock<IPreparation> MockPreparation = new Mock<IPreparation>();
            MockPreparation.Setup(p => p.SelectMode());
            UserInput DummyUserInput = new UserInput("xingqiu", "sword", "3", "y");
            MockPreparation.Setup(p => p.Startup()).Returns(DummyUserInput);
            // SettingFileReader
            Mock<ISettingFileReader> MockSettingFileReader = new Mock<ISettingFileReader>();
            List<ArtifactData> DummyArtifactList = new List<ArtifactData>();
            DummyArtifactList.Add(new ArtifactData("1", "esof", "4pc"));
            DummyArtifactList.Add(new ArtifactData("0", "gd", "hod"));
            MockSettingFileReader.Setup(sfr => sfr.GetArtifactList(null)).Returns(DummyArtifactList);
            List<WeaponData> DummyWeaponList = new List<WeaponData>();
            DummyWeaponList.Add(new WeaponData("霧切の廻光", "mistsplitterreforged", "1"));
            DummyWeaponList.Add(new WeaponData("風鷹剣", "aquilafavonia", "1"));
            DummyWeaponList.Add(new WeaponData("斬山の刃", "summitshaper", "1"));
            MockSettingFileReader.Setup(sfr => sfr.GetWeaponList(DummyUserInput, null)).Returns(DummyWeaponList);
            MockSettingFileReader.Setup(sfr => sfr.GetTextFileContent(It.IsAny<string>(), null)).Returns("");
            //GcsimManager
            Mock<IGcsimManager> MockGcsimManager = new Mock<IGcsimManager>();
            Mock<IGcsim> MockGcsim = new Mock<IGcsim>();
            var sequence = MockGcsim.SetupSequence(g => g.Exec(null));
            // test/bin/Debug/net6.0から見たパス
            string[] DummyGcsimOutputPathList = new[]
            {
                "../../../dummyText/gcsimOutput/esof_1.txt",
                "../../../dummyText/gcsimOutput/esof_2.txt",
                "../../../dummyText/gcsimOutput/esof_3.txt",
                "../../../dummyText/gcsimOutput/gd_hod_1.txt",
                "../../../dummyText/gcsimOutput/gd_hod_2.txt",
                "../../../dummyText/gcsimOutput/gd_hod_3.txt",
            };
            foreach (string DummyGcsimOutputPath in DummyGcsimOutputPathList )
            {
                using (StreamReader TextReader = new StreamReader(DummyGcsimOutputPath))
                {
                    sequence.Returns(TextReader.ReadToEnd());
                }
            }
            MockGcsimManager.Setup(gm => gm.CreateGcsimInstance()).Returns(MockGcsim.Object);   
            //SettingFileWriter
            List<DataTable> ActualDataTables = new List<DataTable>();
            Mock<ISettingFileWriter> MockSettingFileWriter = new Mock<ISettingFileWriter>();
            MockSettingFileWriter.Setup(sfw => sfw.WriteText(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), null));
            MockSettingFileWriter.Setup(sfw => sfw.ExportDataTableToCsv(It.IsAny<DataTable>(), It.IsAny<string>(), null))
                .Callback<DataTable, string, IStreamWriterFactory?>((dt, str, swf) => ActualDataTables.Add(dt.Copy()));
            Mock<_File> MockFile = new Mock<_File>();
            _File.Current = MockFile.Object;
            //update singleton
            PreparationFieldInfo?.SetValue(null, MockPreparation.Object);
            SettingFileReaderFieldInfo?.SetValue(null, MockSettingFileReader.Object);
            SettingFileWriterFieldInfo?.SetValue(null, MockSettingFileWriter.Object);
            SettingFileWriterFieldInfo?.SetValue(null, MockSettingFileWriter.Object);
            GcsimManagerFieldInfo?.SetValue(null, MockGcsimManager.Object);
            //esof
            DataTable ExpectEsofOutputDataTable = new DataTable("Table");
            ExpectEsofOutputDataTable.Columns.Add("武器名");
            ExpectEsofOutputDataTable.Columns.Add("精錬R");
            ExpectEsofOutputDataTable.Columns.Add("DPS");
            ExpectEsofOutputDataTable.Rows.Add("霧切の廻光", "3", "7198.71");
            ExpectEsofOutputDataTable.Rows.Add("風鷹剣", "3", "7133.01");
            ExpectEsofOutputDataTable.Rows.Add("斬山の刃", "3", "7141.6");
            //gd+hod
            DataTable ExpectGdHodOutputDataTable = new DataTable("Table");
            ExpectGdHodOutputDataTable.Columns.Add("武器名");
            ExpectGdHodOutputDataTable.Columns.Add("精錬R");
            ExpectGdHodOutputDataTable.Columns.Add("DPS");
            ExpectGdHodOutputDataTable.Rows.Add("霧切の廻光", "3", "7168.19");
            ExpectGdHodOutputDataTable.Rows.Add("風鷹剣", "3", "7154.78");
            ExpectGdHodOutputDataTable.Rows.Add("斬山の刃", "3", "7175.12");

            Primary.Main();

            MockSettingFileWriter.Verify(sfw => sfw.ExportDataTableToCsv(It.IsAny<DataTable>(), CalcsheetGenerator.Config.Path.Directory.Out + "WeaponDps_esof_4pc.csv", null),
                Times.Once);
            //Columnsチェック
            List<string> ExpectEsofOutputCoulums = (List<string>)ExpectEsofOutputDataTable.Columns.Cast<DataColumn>()
                .Select(c => c.Caption)
                .Select(field => field.ToString()).ToList<string>();
            List<string> ActualEsofOutputCoulums = ActualDataTables[0].Columns.Cast<DataColumn>()
                .Select(c => c.Caption)
                .Select(field => field.ToString()).ToList<string>();
            Assert.All(ExpectEsofOutputCoulums, (expect, Index) => Assert.Equal(expect, ActualEsofOutputCoulums[Index]));
            //Rowsチェック
            List<List<string?>> ExpectEsofOutputRows = ExpectEsofOutputDataTable.Rows.Cast<DataRow>()
                .Select(r => r.ItemArray
                    .Select(i => i?.ToString())
                    .Select(field => field?.ToString()).ToList<string?>()).ToList<List<string?>>();
            List<List<string?>>  ActualEsofOutputRows = ActualDataTables[0].Rows.Cast<DataRow>()
                .Select(r => r.ItemArray
                    .Select(i => i?.ToString())
                    .Select(field => field?.ToString()).ToList<string?>()).ToList<List<string?>>();
            foreach (var (expectRow, rowIndex) in  ExpectEsofOutputRows.Select((row, index) => (row, index)))
            {
                Assert.All(expectRow, (expect, colIndex) => Assert.Equal(expect, ActualEsofOutputRows[rowIndex][colIndex]));
            }

            MockSettingFileWriter.Verify(sfw => sfw.ExportDataTableToCsv(It.IsAny<DataTable>(), CalcsheetGenerator.Config.Path.Directory.Out + "WeaponDps_gd_hod.csv", null),
                Times.Once);
            //Columnsチェック
            List<string> ExpectGdHodOutputCoulums = (List<string>)ExpectGdHodOutputDataTable.Columns.Cast<DataColumn>()
                .Select(c => c.Caption)
                .Select(field => field.ToString()).ToList<string>();
            List<string> ActualGdHodOutputCoulums = ActualDataTables[0].Columns.Cast<DataColumn>()
                .Select(c => c.Caption)
                .Select(field => field.ToString()).ToList<string>();
            Assert.All(ExpectGdHodOutputCoulums, (expect, Index) => Assert.Equal(expect, ActualGdHodOutputCoulums[Index]));
            //Rowsチェック
            List<List<string?>> ExpectGdHodOutputRows = ExpectGdHodOutputDataTable.Rows.Cast<DataRow>()
                .Select(r => r.ItemArray
                    .Select(i => i?.ToString())
                    .Select(field => field?.ToString()).ToList<string?>()).ToList<List<string?>>();
            List<List<string?>>  ActualGdHodOutputRows = ActualDataTables[0].Rows.Cast<DataRow>()
                .Select(r => r.ItemArray
                    .Select(i => i?.ToString())
                    .Select(field => field?.ToString()).ToList<string?>()).ToList<List<string?>>();
            foreach (var (expectRow, rowIndex) in  ExpectEsofOutputRows.Select((row, index) => (row, index)))
            {
                Assert.All(expectRow, (expect, colIndex) => Assert.Equal(expect, ActualGdHodOutputRows[rowIndex][colIndex]));
            }
        }

        [Fact(DisplayName="聖遺物最適化モードなしでの起動")]
        public void NomalOnlyWeaopon()
        {
            FieldInfo? PreparationFieldInfo = typeof(Primary).GetField("_Preparation", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static );
            FieldInfo? SettingFileReaderFieldInfo = typeof(Primary).GetField("_SettingFileReader", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static );
            FieldInfo? SettingFileWriterFieldInfo = typeof(Primary).GetField("_SettingFileWriter", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static );
            FieldInfo? GcsimManagerFieldInfo = typeof(Primary).GetField("_GcsimManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static );
            // Preparation
            Mock<IPreparation> MockPreparation = new Mock<IPreparation>();
            MockPreparation.Setup(p => p.SelectMode());
            UserInput DummyUserInput = new UserInput("xingqiu", "sword", "0", "n");
            MockPreparation.Setup(p => p.Startup()).Returns(DummyUserInput);
            // SettingFileReader
            Mock<ISettingFileReader> MockSettingFileReader = new Mock<ISettingFileReader>();
            List<WeaponData> DummyWeaponList = new List<WeaponData>();
            DummyWeaponList.Add(new WeaponData("霧切の廻光", "mistsplitterreforged", "1"));
            DummyWeaponList.Add(new WeaponData("風鷹剣", "aquilafavonia", "1"));
            DummyWeaponList.Add(new WeaponData("斬山の刃", "summitshaper", "1"));
            MockSettingFileReader.Setup(sfr => sfr.GetWeaponList(DummyUserInput, null)).Returns(DummyWeaponList);
            MockSettingFileReader.Setup(sfr => sfr.GetTextFileContent(It.IsAny<string>(), null)).Returns("");
            //GcsimManager
            Mock<IGcsimManager> MockGcsimManager = new Mock<IGcsimManager>();
            Mock<IGcsim> MockGcsim = new Mock<IGcsim>();
            var sequence = MockGcsim.SetupSequence(g => g.Exec(null));
            // test/bin/Debug/net6.0から見たパス
            string[] DummyGcsimOutputPathList = new[]
            {
                "../../../dummyText/gcsimOutput/esof_1.txt",
                "../../../dummyText/gcsimOutput/esof_2.txt",
                "../../../dummyText/gcsimOutput/esof_3.txt",
            };
            foreach (string DummyGcsimOutputPath in DummyGcsimOutputPathList )
            {
                using (StreamReader TextReader = new StreamReader(DummyGcsimOutputPath))
                {
                    sequence.Returns(TextReader.ReadToEnd());
                }
            }
            MockGcsimManager.Setup(gm => gm.CreateGcsimInstance()).Returns(MockGcsim.Object);   
            //SettingFileWriter
            List<DataTable> ActualDataTables = new List<DataTable>();
            Mock<ISettingFileWriter> MockSettingFileWriter = new Mock<ISettingFileWriter>();
            MockSettingFileWriter.Setup(sfw => sfw.WriteText(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), null));
            MockSettingFileWriter.Setup(sfw => sfw.ExportDataTableToCsv(It.IsAny<DataTable>(), It.IsAny<string>(), null))
                .Callback<DataTable, string, IStreamWriterFactory?>((dt, str, swf) => ActualDataTables.Add(dt.Copy()));
            Mock<_File> MockFile = new Mock<_File>();
            _File.Current = MockFile.Object;
            //update singleton
            PreparationFieldInfo?.SetValue(null, MockPreparation.Object);
            SettingFileReaderFieldInfo?.SetValue(null, MockSettingFileReader.Object);
            SettingFileWriterFieldInfo?.SetValue(null, MockSettingFileWriter.Object);
            SettingFileWriterFieldInfo?.SetValue(null, MockSettingFileWriter.Object);
            GcsimManagerFieldInfo?.SetValue(null, MockGcsimManager.Object);
            //nonartifact
            DataTable ExpectEsofOutputDataTable = new DataTable("Table");
            ExpectEsofOutputDataTable.Columns.Add("武器名");
            ExpectEsofOutputDataTable.Columns.Add("精錬R");
            ExpectEsofOutputDataTable.Columns.Add("DPS");
            ExpectEsofOutputDataTable.Rows.Add("霧切の廻光", "1", "7198.71");
            ExpectEsofOutputDataTable.Rows.Add("風鷹剣", "1", "7133.01");
            ExpectEsofOutputDataTable.Rows.Add("斬山の刃", "1", "7141.6");

            Primary.Main();

            MockSettingFileWriter.Verify(sfw => sfw.ExportDataTableToCsv(It.IsAny<DataTable>(), CalcsheetGenerator.Config.Path.Directory.Out + "WeaponDps.csv", null),
                Times.Once);
            //Columnsチェック
            List<string> ExpectEsofOutputCoulums = (List<string>)ExpectEsofOutputDataTable.Columns.Cast<DataColumn>()
                .Select(c => c.Caption)
                .Select(field => field.ToString()).ToList<string>();
            List<string> ActualEsofOutputCoulums = ActualDataTables[0].Columns.Cast<DataColumn>()
                .Select(c => c.Caption)
                .Select(field => field.ToString()).ToList<string>();
            Assert.All(ExpectEsofOutputCoulums, (expect, Index) => Assert.Equal(expect, ActualEsofOutputCoulums[Index]));
            //Rowsチェック
            List<List<string?>> ExpectEsofOutputRows = ExpectEsofOutputDataTable.Rows.Cast<DataRow>()
                .Select(r => r.ItemArray
                    .Select(i => i?.ToString())
                    .Select(field => field?.ToString()).ToList<string?>()).ToList<List<string?>>();
            List<List<string?>>  ActualEsofOutputRows = ActualDataTables[0].Rows.Cast<DataRow>()
                .Select(r => r.ItemArray
                    .Select(i => i?.ToString())
                    .Select(field => field?.ToString()).ToList<string?>()).ToList<List<string?>>();
            foreach (var (expectRow, rowIndex) in  ExpectEsofOutputRows.Select((row, index) => (row, index)))
            {
                Assert.All(expectRow, (expect, colIndex) => Assert.Equal(expect, ActualEsofOutputRows[rowIndex][colIndex]));
            }
        }

        [Fact(DisplayName="聖遺物最適化モードなしかつ武器の手動精錬ランク設定での起動")]
        public void NomalWeaoponAutoRefineNumber()
        {
            FieldInfo? PreparationFieldInfo = typeof(Primary).GetField("_Preparation", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static );
            FieldInfo? SettingFileReaderFieldInfo = typeof(Primary).GetField("_SettingFileReader", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static );
            FieldInfo? SettingFileWriterFieldInfo = typeof(Primary).GetField("_SettingFileWriter", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static );
            FieldInfo? GcsimManagerFieldInfo = typeof(Primary).GetField("_GcsimManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static );
            // Preparation
            Mock<IPreparation> MockPreparation = new Mock<IPreparation>();
            MockPreparation.Setup(p => p.SelectMode());
            UserInput DummyUserInput = new UserInput("xingqiu", "sword", "3", "n");
            MockPreparation.Setup(p => p.Startup()).Returns(DummyUserInput);
            // SettingFileReader
            Mock<ISettingFileReader> MockSettingFileReader = new Mock<ISettingFileReader>();
            List<WeaponData> DummyWeaponList = new List<WeaponData>();
            DummyWeaponList.Add(new WeaponData("霧切の廻光", "mistsplitterreforged", "1"));
            DummyWeaponList.Add(new WeaponData("風鷹剣", "aquilafavonia", "1"));
            DummyWeaponList.Add(new WeaponData("斬山の刃", "summitshaper", "1"));
            MockSettingFileReader.Setup(sfr => sfr.GetWeaponList(DummyUserInput, null)).Returns(DummyWeaponList);
            MockSettingFileReader.Setup(sfr => sfr.GetTextFileContent(It.IsAny<string>(), null)).Returns("");
            //GcsimManager
            Mock<IGcsimManager> MockGcsimManager = new Mock<IGcsimManager>();
            Mock<IGcsim> MockGcsim = new Mock<IGcsim>();
            var sequence = MockGcsim.SetupSequence(g => g.Exec(null));
            // test/bin/Debug/net6.0から見たパス
            string[] DummyGcsimOutputPathList = new[]
            {
                "../../../dummyText/gcsimOutput/esof_1.txt",
                "../../../dummyText/gcsimOutput/esof_2.txt",
                "../../../dummyText/gcsimOutput/esof_3.txt",
            };
            foreach (string DummyGcsimOutputPath in DummyGcsimOutputPathList )
            {
                using (StreamReader TextReader = new StreamReader(DummyGcsimOutputPath))
                {
                    sequence.Returns(TextReader.ReadToEnd());
                }
            }
            MockGcsimManager.Setup(gm => gm.CreateGcsimInstance()).Returns(MockGcsim.Object);   
            //SettingFileWriter
            List<DataTable> ActualDataTables = new List<DataTable>();
            Mock<ISettingFileWriter> MockSettingFileWriter = new Mock<ISettingFileWriter>();
            MockSettingFileWriter.Setup(sfw => sfw.WriteText(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), null));
            MockSettingFileWriter.Setup(sfw => sfw.ExportDataTableToCsv(It.IsAny<DataTable>(), It.IsAny<string>(), null))
                .Callback<DataTable, string, IStreamWriterFactory?>((dt, str, swf) => ActualDataTables.Add(dt.Copy()));
            Mock<_File> MockFile = new Mock<_File>();
            _File.Current = MockFile.Object;
            //update singleton
            PreparationFieldInfo?.SetValue(null, MockPreparation.Object);
            SettingFileReaderFieldInfo?.SetValue(null, MockSettingFileReader.Object);
            SettingFileWriterFieldInfo?.SetValue(null, MockSettingFileWriter.Object);
            SettingFileWriterFieldInfo?.SetValue(null, MockSettingFileWriter.Object);
            GcsimManagerFieldInfo?.SetValue(null, MockGcsimManager.Object);
            //nonartifact
            DataTable ExpectEsofOutputDataTable = new DataTable("Table");
            ExpectEsofOutputDataTable.Columns.Add("武器名");
            ExpectEsofOutputDataTable.Columns.Add("精錬R");
            ExpectEsofOutputDataTable.Columns.Add("DPS");
            ExpectEsofOutputDataTable.Rows.Add("霧切の廻光", "3", "7198.71");
            ExpectEsofOutputDataTable.Rows.Add("風鷹剣", "3", "7133.01");
            ExpectEsofOutputDataTable.Rows.Add("斬山の刃", "3", "7141.6");

            Primary.Main();

            MockSettingFileWriter.Verify(sfw => sfw.ExportDataTableToCsv(It.IsAny<DataTable>(), CalcsheetGenerator.Config.Path.Directory.Out + "WeaponDps.csv", null),
                Times.Once);
            //Columnsチェック
            List<string> ExpectEsofOutputCoulums = (List<string>)ExpectEsofOutputDataTable.Columns.Cast<DataColumn>()
                .Select(c => c.Caption)
                .Select(field => field.ToString()).ToList<string>();
            List<string> ActualEsofOutputCoulums = ActualDataTables[0].Columns.Cast<DataColumn>()
                .Select(c => c.Caption)
                .Select(field => field.ToString()).ToList<string>();
            Assert.All(ExpectEsofOutputCoulums, (expect, Index) => Assert.Equal(expect, ActualEsofOutputCoulums[Index]));
            //Rowsチェック
            List<List<string?>> ExpectEsofOutputRows = ExpectEsofOutputDataTable.Rows.Cast<DataRow>()
                .Select(r => r.ItemArray
                    .Select(i => i?.ToString())
                    .Select(field => field?.ToString()).ToList<string?>()).ToList<List<string?>>();
            List<List<string?>>  ActualEsofOutputRows = ActualDataTables[0].Rows.Cast<DataRow>()
                .Select(r => r.ItemArray
                    .Select(i => i?.ToString())
                    .Select(field => field?.ToString()).ToList<string?>()).ToList<List<string?>>();
            foreach (var (expectRow, rowIndex) in  ExpectEsofOutputRows.Select((row, index) => (row, index)))
            {
                Assert.All(expectRow, (expect, colIndex) => Assert.Equal(expect, ActualEsofOutputRows[rowIndex][colIndex]));
            }
        }

        [Fact(DisplayName="Gcsim.Execで例外が発生した場合にDPS:0として処理が継続する事")]
        public void NomalRefailGcsimExec()
        {
            FieldInfo? PreparationFieldInfo = typeof(Primary).GetField("_Preparation", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static );
            FieldInfo? SettingFileReaderFieldInfo = typeof(Primary).GetField("_SettingFileReader", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static );
            FieldInfo? SettingFileWriterFieldInfo = typeof(Primary).GetField("_SettingFileWriter", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static );
            FieldInfo? GcsimManagerFieldInfo = typeof(Primary).GetField("_GcsimManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static );
            // Preparation
            Mock<IPreparation> MockPreparation = new Mock<IPreparation>();
            MockPreparation.Setup(p => p.SelectMode());
            UserInput DummyUserInput = new UserInput("xingqiu", "sword", "0", "n");
            MockPreparation.Setup(p => p.Startup()).Returns(DummyUserInput);
            // SettingFileReader
            Mock<ISettingFileReader> MockSettingFileReader = new Mock<ISettingFileReader>();
            List<WeaponData> DummyWeaponList = new List<WeaponData>();
            DummyWeaponList.Add(new WeaponData("霧切の廻光", "mistsplitterreforged", "1"));
            DummyWeaponList.Add(new WeaponData("風鷹剣", "aquilafavonia", "1"));
            DummyWeaponList.Add(new WeaponData("斬山の刃", "summitshaper", "1"));
            MockSettingFileReader.Setup(sfr => sfr.GetWeaponList(DummyUserInput, null)).Returns(DummyWeaponList);
            MockSettingFileReader.Setup(sfr => sfr.GetTextFileContent(It.IsAny<string>(), null)).Returns("");
            //GcsimManager
            Mock<IGcsimManager> MockGcsimManager = new Mock<IGcsimManager>();
            Mock<IGcsim> MockGcsim = new Mock<IGcsim>();
            var sequence = MockGcsim.SetupSequence(g => g.Exec(null));
            // test/bin/Debug/net6.0から見たパス
            string[] DummyGcsimOutputPathList = new[]
            {
                "../../../dummyText/gcsimOutput/esof_1.txt",
                "../../../dummyText/gcsimOutput/esof_3.txt",
            };
            List<string> ReturnOutputText = new List<string>();
            foreach (string DummyGcsimOutputPath in DummyGcsimOutputPathList )
            {
                using (StreamReader TextReader = new StreamReader(DummyGcsimOutputPath))
                {
                    ReturnOutputText.Add(TextReader.ReadToEnd());
                }
            }
            sequence.Returns(ReturnOutputText[0])
                    .Throws(new Exception(Message.Error.GcsimOutputNone))
                    .Returns(ReturnOutputText[1]);
            MockGcsimManager.Setup(gm => gm.CreateGcsimInstance()).Returns(MockGcsim.Object);   
            //SettingFileWriter
            List<DataTable> ActualDataTables = new List<DataTable>();
            Mock<ISettingFileWriter> MockSettingFileWriter = new Mock<ISettingFileWriter>();
            MockSettingFileWriter.Setup(sfw => sfw.WriteText(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), null));
            MockSettingFileWriter.Setup(sfw => sfw.ExportDataTableToCsv(It.IsAny<DataTable>(), It.IsAny<string>(), null))
                .Callback<DataTable, string, IStreamWriterFactory?>((dt, str, swf) => ActualDataTables.Add(dt.Copy()));
            Mock<_File> MockFile = new Mock<_File>();
            _File.Current = MockFile.Object;
            //update singleton
            PreparationFieldInfo?.SetValue(null, MockPreparation.Object);
            SettingFileReaderFieldInfo?.SetValue(null, MockSettingFileReader.Object);
            SettingFileWriterFieldInfo?.SetValue(null, MockSettingFileWriter.Object);
            SettingFileWriterFieldInfo?.SetValue(null, MockSettingFileWriter.Object);
            GcsimManagerFieldInfo?.SetValue(null, MockGcsimManager.Object);
            //nonartifact
            DataTable ExpectEsofOutputDataTable = new DataTable("Table");
            ExpectEsofOutputDataTable.Columns.Add("武器名");
            ExpectEsofOutputDataTable.Columns.Add("精錬R");
            ExpectEsofOutputDataTable.Columns.Add("DPS");
            ExpectEsofOutputDataTable.Rows.Add("霧切の廻光", "1", "7198.71");
            ExpectEsofOutputDataTable.Rows.Add("風鷹剣", "1", "0");
            ExpectEsofOutputDataTable.Rows.Add("斬山の刃", "1", "7141.6");

            Primary.Main();

            MockSettingFileWriter.Verify(sfw => sfw.ExportDataTableToCsv(It.IsAny<DataTable>(), CalcsheetGenerator.Config.Path.Directory.Out + "WeaponDps.csv", null),
                Times.Once);
            //Columnsチェック
            List<string> ExpectEsofOutputCoulums = (List<string>)ExpectEsofOutputDataTable.Columns.Cast<DataColumn>()
                .Select(c => c.Caption)
                .Select(field => field.ToString()).ToList<string>();
            List<string> ActualEsofOutputCoulums = ActualDataTables[0].Columns.Cast<DataColumn>()
                .Select(c => c.Caption)
                .Select(field => field.ToString()).ToList<string>();
            Assert.All(ExpectEsofOutputCoulums, (expect, Index) => Assert.Equal(expect, ActualEsofOutputCoulums[Index]));
            //Rowsチェック
            List<List<string?>> ExpectEsofOutputRows = ExpectEsofOutputDataTable.Rows.Cast<DataRow>()
                .Select(r => r.ItemArray
                    .Select(i => i?.ToString())
                    .Select(field => field?.ToString()).ToList<string?>()).ToList<List<string?>>();
            List<List<string?>>  ActualEsofOutputRows = ActualDataTables[0].Rows.Cast<DataRow>()
                .Select(r => r.ItemArray
                    .Select(i => i?.ToString())
                    .Select(field => field?.ToString()).ToList<string?>()).ToList<List<string?>>();
            foreach (var (expectRow, rowIndex) in  ExpectEsofOutputRows.Select((row, index) => (row, index)))
            {
                Assert.All(expectRow, (expect, colIndex) => Assert.Equal(expect, ActualEsofOutputRows[rowIndex][colIndex]));
            }
        }

        [Theory(DisplayName="FormatException例外発生でアプリケーションが強制終了すること")]
        [InlineData("", "sword")]
        [InlineData("Jean", "")]
        public void ErrorFormatException(string CharacterName, string WeaponType)
        {
            FieldInfo? PreparationFieldInfo = typeof(Primary).GetField("_Preparation", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static );
            // Preparation
            Mock<IPreparation> MockPreparation = new Mock<IPreparation>();
            MockPreparation.Setup(p => p.SelectMode());
            UserInput DummyUserInput = new UserInput(CharacterName, WeaponType, "0", "y");
            MockPreparation.Setup(p => p.Startup()).Returns(DummyUserInput);
            PreparationFieldInfo?.SetValue(null, MockPreparation.Object);
            Mock<_Environment> MockEnviroment = new Mock<_Environment>();
            _Environment.Current = MockEnviroment.Object;

            Primary.Main();
            
            MockEnviroment.Verify(e => e.Exit(1));
        }
    }
}