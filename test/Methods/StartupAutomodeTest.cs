using System;

using CalcsheetGenerator;

namespace Methods
{
    public class StartupAutomodeTest : IDisposable
    {
        //setup
        public StartupAutomodeTest()
        {
            var output = new StringWriter();
            Console.SetOut(output);
        }

        // teardown
        public void Dispose()
        {
            //pass
        }

        [Fact(DisplayName="正常パターン")]
        public void Nomal()
        {
            var input = new StringReader("Jean\nsword");
            Console.SetIn(input);

            string[] expectOutput = new String[] {"Jean", "sword", "0", "y"};
            Assert.Equal(expectOutput, AWC.Startup_automode());
        }

        [Theory(DisplayName="未入力が含まれるパターンで強制終了していること")]
        [InlineData("Jean", "")]
        [InlineData("", "sword")]
        [InlineData("", "")]
        public void ErrorNullInput(string charactorName, string weaponType)
        {
            var input = new StringReader($"{charactorName}\n{weaponType}\n");
            Console.SetIn(input);

            var enviromnent = new Mock<_Environment>();
            _Environment.Current = enviromnent.Object;
            AWC.Startup_automode();
            enviromnent.Verify(mock => mock.Exit(0), Times.Once);
        }
    }
}

