using System.Text;

using CalcsheetGenerator;
using CalcsheetGenerator.Interfaces;

namespace Test.Methods
{
    public class WriteTextTest
    {
        static readonly SettingFileWriter _SettingFileWriter = SettingFileWriter.GetInstance();

        [Fact(DisplayName="書き込み処理が行われること")]
        public void Nomal()
        {
            Mock<IStreamWriter> MockStreamWriter = new Mock<IStreamWriter>();
            MockStreamWriter.Setup(sw => sw.Write(It.IsAny<string>())).Callback(() => {});
            Mock<IStreamWriterFactory> MockStreamWriterFactory= new Mock<IStreamWriterFactory>();
            MockStreamWriterFactory.Setup(swf => swf.Create(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Encoding>()))
                .Returns(MockStreamWriter.Object);
            
            _SettingFileWriter.WriteText(CalcsheetGenerator.Config.Path.File.SimConfigText, false, "test", MockStreamWriterFactory.Object);

            MockStreamWriter.Verify(sw => sw.Write("test"), Times.Once);
        }
    }
}