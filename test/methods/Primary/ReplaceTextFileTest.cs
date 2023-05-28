using System.Reflection;

using CalcsheetGenerator;
using CalcsheetGenerator.Interfaces;

namespace Test.Methods
{
    public class ReplaceTextFileTest
    {
        [Fact(DisplayName="コンテンツを読み込み、リプレイス処理の後書き込み処理が行われること")]
        public void Nomal()
        {
            FieldInfo? SettingFileReaderFieldInfo = typeof(Primary).GetField("_SettingFileReader", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static );
            FieldInfo? SettingFileWriterFieldInfo = typeof(Primary).GetField("_SettingFileWriter", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static );
            Mock<ISettingFileReader> MockSettingFileReader = new Mock<ISettingFileReader>();
            MockSettingFileReader.Setup(sfr => sfr.GetTextFileContet(It.IsAny<string>(), null)).Returns("test");
            Mock<ISettingFileWriter> MockSettingFileWriter = new Mock<ISettingFileWriter>();
            MockSettingFileWriter.Setup(sfw => sfw.WriteText(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), null)).Callback(()=>{});
            SettingFileReaderFieldInfo?.SetValue(null, MockSettingFileReader.Object);
            SettingFileWriterFieldInfo?.SetValue(null, MockSettingFileWriter.Object);


            Primary.ReplaceTextFile(CalcsheetGenerator.Config.Path.File.SimConfigText, "test", "answer");

            MockSettingFileWriter.Verify(sfw => sfw.WriteText(CalcsheetGenerator.Config.Path.File.SimConfigText, false, "answer", null),
                Times.Once);
        }
    }
}