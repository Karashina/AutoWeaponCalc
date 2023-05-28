using CalcsheetGenerator;
using CalcsheetGenerator.Interfaces;

namespace Test.Methods
{
    public class ExecTest
    {
        Mock<IProcessFactory> MockProcess = new Mock<IProcessFactory>();
        Mock<IGcsimProcess> MockGcsimProcess = new Mock<IGcsimProcess>();
        // setup
        public ExecTest()
        {
            MockGcsimProcess.Setup(gp => gp.Start()).Callback(() => {});
            MockGcsimProcess.Setup(gp => gp.WaitForExit()).Callback(() => {});
        }

        [Fact(DisplayName="出力がある場合その文字列をそのまま出力")]
        public void Nomal()
        {
            string expectedOutput = "hello";
            MockGcsimProcess.Setup(gp => gp.GetOuptput()).Returns(() => "hello");
            MockProcess.Setup(p => p.Create(It.IsAny<string[]>())).Returns(MockGcsimProcess.Object);

            Gcsim _Gcsim = new Gcsim();
            string actualOutput = _Gcsim.Exec(MockProcess.Object);

            Assert.Equal(expectedOutput, actualOutput);
        }

        [Fact(DisplayName="出力がNullの場合、例外を投げること")]
        public void ErrorOutputNullThrowException()
        {
            MockGcsimProcess.Setup(gp => gp.GetOuptput()).Returns(() => null);
            MockProcess.Setup(p => p.Create(It.IsAny<string[]>())).Returns(MockGcsimProcess.Object);

            Gcsim _Gcsim = new Gcsim();
            var exception = Assert.Throws<Exception>(() => _Gcsim.Exec(MockProcess.Object));

            Assert.NotEmpty(exception.Message);
        }

        [Fact(DisplayName="出力が空文字の場合、例外を投げること")]
        public void ErrorOutputEmptyThrowException()
        {
            MockGcsimProcess.Setup(gp => gp.GetOuptput()).Returns(() => "");
            MockProcess.Setup(p => p.Create(It.IsAny<string[]>())).Returns(MockGcsimProcess.Object);

            Gcsim _Gcsim = new Gcsim();
            var exception = Assert.Throws<Exception>(() => _Gcsim.Exec(MockProcess.Object));

            Assert.NotEmpty(exception.Message);
        }
    }
}